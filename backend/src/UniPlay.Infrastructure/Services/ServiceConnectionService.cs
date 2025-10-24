using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniPlay.Core.DTOs;
using UniPlay.Core.Entities;
using UniPlay.Core.Enums;
using UniPlay.Core.Services;
using UniPlay.Infrastructure.Data;

namespace UniPlay.Infrastructure.Services;

/// <summary>
/// Service for managing user connections to music streaming services
/// </summary>
public class ServiceConnectionService : IServiceConnectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenEncryptionService _tokenEncryption;
    private readonly ILogger<ServiceConnectionService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private const int MaxConnectionsPerService = 3;

    public ServiceConnectionService(
        ApplicationDbContext context,
        ITokenEncryptionService tokenEncryption,
        IServiceProvider serviceProvider,
        ILogger<ServiceConnectionService> logger)
    {
        _context = context;
        _tokenEncryption = tokenEncryption;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private IMusicServiceAdapter GetAdapter(ServiceType serviceType)
    {
        return _serviceProvider.GetRequiredKeyedService<IMusicServiceAdapter>(serviceType);
    }

    public async Task<OAuthUrlResponse> InitiateOAuthAsync(Guid userId, InitiateOAuthRequest request)
    {
        _logger.LogInformation("Initiating OAuth for user {UserId} and service {ServiceType}",
            userId, request.ServiceType);

        // Check if user can add more connections
        if (!await CanAddConnectionAsync(userId, request.ServiceType))
        {
            throw new InvalidOperationException(
                $"Maximum number of connections ({MaxConnectionsPerService}) reached for {request.ServiceType}");
        }

        // Get the appropriate adapter
        var adapter = GetAdapter(request.ServiceType);

        // Generate state token (userId + random guid for security)
        var state = $"{userId}:{Guid.NewGuid()}";
        var redirectUri = request.RedirectUri ?? GetDefaultRedirectUri(request.ServiceType);

        // Generate authorization URL
        var authUrl = adapter.GenerateAuthorizationUrl(state, redirectUri);

        return new OAuthUrlResponse
        {
            AuthorizationUrl = authUrl,
            State = state
        };
    }

    public async Task<CompleteOAuthResponse> CompleteOAuthAsync(Guid userId, CompleteOAuthRequest request)
    {
        _logger.LogInformation("Completing OAuth for user {UserId} and service {ServiceType}",
            userId, request.ServiceType);

        // Validate state token
        if (!ValidateState(request.State, userId))
        {
            throw new InvalidOperationException("Invalid state token - possible CSRF attack");
        }

        // Get the appropriate adapter
        var adapter = GetAdapter(request.ServiceType);

        var redirectUri = request.RedirectUri ?? GetDefaultRedirectUri(request.ServiceType);

        // Exchange code for tokens
        var tokenResult = await adapter.ExchangeCodeForTokenAsync(request.Code, redirectUri);

        // Get user profile from service
        var profile = await adapter.GetUserProfileAsync(tokenResult.AccessToken);

        // Check if connection already exists for this service account
        var existingConnection = await _context.ServiceConnections
            .FirstOrDefaultAsync(sc =>
                sc.UserId == userId &&
                sc.ServiceType == request.ServiceType &&
                sc.ServiceAccountId == profile.ServiceAccountId);

        ServiceConnection connection;

        if (existingConnection != null)
        {
            // Update existing connection
            existingConnection.EncryptedAccessToken = _tokenEncryption.Encrypt(tokenResult.AccessToken);
            existingConnection.EncryptedRefreshToken = tokenResult.RefreshToken != null
                ? _tokenEncryption.Encrypt(tokenResult.RefreshToken)
                : null;
            existingConnection.AccessTokenExpiresAt = tokenResult.ExpiresAt;
            existingConnection.Scopes = tokenResult.Scopes;
            existingConnection.ConnectionStatus = ConnectionStatus.Active;
            existingConnection.ServiceAccountEmail = profile.Email;
            existingConnection.ServiceAccountDisplayName = profile.DisplayName;
            existingConnection.ServiceAccountProfileImageUrl = profile.ProfileImageUrl;
            existingConnection.UpdatedAt = DateTime.UtcNow;

            connection = existingConnection;
            _logger.LogInformation("Updated existing connection {ConnectionId}", connection.Id);
        }
        else
        {
            // Create new connection
            connection = new ServiceConnection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ServiceType = request.ServiceType,
                ServiceAccountId = profile.ServiceAccountId,
                ServiceAccountEmail = profile.Email,
                ServiceAccountDisplayName = profile.DisplayName,
                ServiceAccountProfileImageUrl = profile.ProfileImageUrl,
                EncryptedAccessToken = _tokenEncryption.Encrypt(tokenResult.AccessToken),
                EncryptedRefreshToken = tokenResult.RefreshToken != null
                    ? _tokenEncryption.Encrypt(tokenResult.RefreshToken)
                    : null,
                AccessTokenExpiresAt = tokenResult.ExpiresAt,
                Scopes = tokenResult.Scopes,
                ConnectionStatus = ConnectionStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ServiceConnections.Add(connection);
            _logger.LogInformation("Created new connection {ConnectionId}", connection.Id);
        }

        await _context.SaveChangesAsync();

        return new CompleteOAuthResponse
        {
            ConnectionId = connection.Id,
            Connection = MapToDto(connection)
        };
    }

    public async Task<List<ServiceConnectionDto>> GetUserConnectionsAsync(Guid userId)
    {
        var connections = await _context.ServiceConnections
            .Where(sc => sc.UserId == userId)
            .OrderBy(sc => sc.ServiceType)
            .ThenBy(sc => sc.CreatedAt)
            .ToListAsync();

        return connections.Select(MapToDto).ToList();
    }

    public async Task<ServiceConnectionDto?> GetConnectionAsync(Guid userId, Guid connectionId)
    {
        var connection = await _context.ServiceConnections
            .FirstOrDefaultAsync(sc => sc.Id == connectionId && sc.UserId == userId);

        return connection != null ? MapToDto(connection) : null;
    }

    public async Task<List<ServiceConnectionDto>> GetConnectionsByServiceAsync(Guid userId, ServiceType serviceType)
    {
        var connections = await _context.ServiceConnections
            .Where(sc => sc.UserId == userId && sc.ServiceType == serviceType)
            .OrderBy(sc => sc.CreatedAt)
            .ToListAsync();

        return connections.Select(MapToDto).ToList();
    }

    public async Task DisconnectServiceAsync(Guid userId, Guid connectionId)
    {
        var connection = await _context.ServiceConnections
            .FirstOrDefaultAsync(sc => sc.Id == connectionId && sc.UserId == userId);

        if (connection == null)
        {
            throw new InvalidOperationException("Connection not found");
        }

        // Try to revoke token (best effort)
        try
        {
            var adapter = GetAdapter(connection.ServiceType);
            var accessToken = _tokenEncryption.Decrypt(connection.EncryptedAccessToken);
            await adapter.RevokeTokenAsync(accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke token for connection {ConnectionId}", connectionId);
        }

        _context.ServiceConnections.Remove(connection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Disconnected service connection {ConnectionId}", connectionId);
    }

    public async Task RefreshConnectionTokenAsync(Guid connectionId)
    {
        var connection = await _context.ServiceConnections
            .FirstOrDefaultAsync(sc => sc.Id == connectionId);

        if (connection == null)
        {
            throw new InvalidOperationException("Connection not found");
        }

        if (string.IsNullOrEmpty(connection.EncryptedRefreshToken))
        {
            throw new InvalidOperationException("No refresh token available");
        }

        var adapter = GetAdapter(connection.ServiceType);

        try
        {
            var refreshToken = _tokenEncryption.Decrypt(connection.EncryptedRefreshToken);
            var tokenResult = await adapter.RefreshAccessTokenAsync(refreshToken);

            connection.EncryptedAccessToken = _tokenEncryption.Encrypt(tokenResult.AccessToken);
            if (tokenResult.RefreshToken != null)
            {
                connection.EncryptedRefreshToken = _tokenEncryption.Encrypt(tokenResult.RefreshToken);
            }
            connection.AccessTokenExpiresAt = tokenResult.ExpiresAt;
            connection.ConnectionStatus = ConnectionStatus.Active;
            connection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Refreshed access token for connection {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token for connection {ConnectionId}", connectionId);
            connection.ConnectionStatus = ConnectionStatus.Error;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    public async Task<bool> CanAddConnectionAsync(Guid userId, ServiceType serviceType)
    {
        var count = await _context.ServiceConnections
            .CountAsync(sc => sc.UserId == userId && sc.ServiceType == serviceType);

        return count < MaxConnectionsPerService;
    }

    private bool ValidateState(string state, Guid userId)
    {
        var parts = state.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        return Guid.TryParse(parts[0], out var stateUserId) && stateUserId == userId;
    }

    private string GetDefaultRedirectUri(ServiceType serviceType)
    {
        return serviceType switch
        {
            ServiceType.Spotify => "https://localhost:5001/api/v1/services/spotify/callback",
            ServiceType.AppleMusic => "https://localhost:5001/api/v1/services/applemusic/callback",
            ServiceType.Deezer => "https://localhost:5001/api/v1/services/deezer/callback",
            ServiceType.YouTubeMusic => "https://localhost:5001/api/v1/services/youtubemusic/callback",
            _ => throw new NotSupportedException($"Service type {serviceType} is not supported")
        };
    }

    private ServiceConnectionDto MapToDto(ServiceConnection connection)
    {
        return new ServiceConnectionDto
        {
            Id = connection.Id,
            ServiceType = connection.ServiceType,
            ServiceAccountId = connection.ServiceAccountId,
            ServiceAccountEmail = connection.ServiceAccountEmail,
            ServiceAccountDisplayName = connection.ServiceAccountDisplayName,
            ServiceAccountProfileImageUrl = connection.ServiceAccountProfileImageUrl,
            ConnectionStatus = connection.ConnectionStatus,
            LastSyncedAt = connection.LastSyncedAt,
            CreatedAt = connection.CreatedAt,
            AccessTokenExpiresAt = connection.AccessTokenExpiresAt
        };
    }
}

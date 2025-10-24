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
/// Service for managing playlists from music streaming services
/// </summary>
public class PlaylistService : IPlaylistService
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenEncryptionService _tokenEncryption;
    private readonly ILogger<PlaylistService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PlaylistService(
        ApplicationDbContext context,
        ITokenEncryptionService tokenEncryption,
        IServiceProvider serviceProvider,
        ILogger<PlaylistService> logger)
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

    public async Task<List<PlaylistWithConnectionDto>> GetUserPlaylistsAsync(Guid userId)
    {
        var playlists = await _context.ServicePlaylists
            .Include(sp => sp.ServiceConnection)
            .Where(sp => sp.ServiceConnection.UserId == userId)
            .OrderBy(sp => sp.ServiceConnection.ServiceType)
            .ThenBy(sp => sp.Name)
            .ToListAsync();

        return playlists.Select(MapToPlaylistWithConnectionDto).ToList();
    }

    public async Task<List<ServicePlaylistDto>> GetConnectionPlaylistsAsync(Guid userId, Guid connectionId)
    {
        var playlists = await _context.ServicePlaylists
            .Include(sp => sp.ServiceConnection)
            .Where(sp => sp.ServiceConnectionId == connectionId && sp.ServiceConnection.UserId == userId)
            .OrderBy(sp => sp.Name)
            .ToListAsync();

        return playlists.Select(MapToDto).ToList();
    }

    public async Task<SyncPlaylistsResponse> SyncPlaylistsAsync(Guid connectionId)
    {
        var connection = await _context.ServiceConnections
            .FirstOrDefaultAsync(sc => sc.Id == connectionId);

        if (connection == null)
        {
            throw new InvalidOperationException("Connection not found");
        }

        if (connection.ConnectionStatus != ConnectionStatus.Active)
        {
            throw new InvalidOperationException("Connection is not active");
        }

        var adapter = GetAdapter(connection.ServiceType);

        _logger.LogInformation("Syncing playlists for connection {ConnectionId} ({ServiceType})",
            connectionId, connection.ServiceType);

        // Decrypt access token
        var accessToken = _tokenEncryption.Decrypt(connection.EncryptedAccessToken);

        // Fetch playlists from service
        var servicePlaylists = await adapter.GetUserPlaylistsAsync(accessToken);

        // Get existing playlists from database
        var existingPlaylists = await _context.ServicePlaylists
            .Where(sp => sp.ServiceConnectionId == connectionId)
            .ToListAsync();

        var existingPlaylistIds = existingPlaylists.ToDictionary(p => p.ServicePlaylistId);

        var added = 0;
        var updated = 0;
        var syncedAt = DateTime.UtcNow;

        // Add or update playlists
        foreach (var servicePlaylist in servicePlaylists)
        {
            if (existingPlaylistIds.TryGetValue(servicePlaylist.ServicePlaylistId, out var existing))
            {
                // Update existing playlist
                existing.Name = servicePlaylist.Name;
                existing.Description = servicePlaylist.Description;
                existing.ImageUrl = servicePlaylist.ImageUrl;
                existing.TrackCount = servicePlaylist.TrackCount;
                existing.IsPublic = servicePlaylist.IsPublic;
                existing.IsCollaborative = servicePlaylist.IsCollaborative;
                existing.OwnerName = servicePlaylist.OwnerName;
                existing.ServiceUrl = servicePlaylist.ServiceUrl;
                existing.LastSyncedAt = syncedAt;
                existing.UpdatedAt = syncedAt;

                updated++;
                existingPlaylistIds.Remove(servicePlaylist.ServicePlaylistId);
            }
            else
            {
                // Add new playlist
                var newPlaylist = new ServicePlaylist
                {
                    Id = Guid.NewGuid(),
                    ServiceConnectionId = connectionId,
                    ServicePlaylistId = servicePlaylist.ServicePlaylistId,
                    Name = servicePlaylist.Name,
                    Description = servicePlaylist.Description,
                    ImageUrl = servicePlaylist.ImageUrl,
                    TrackCount = servicePlaylist.TrackCount,
                    IsPublic = servicePlaylist.IsPublic,
                    IsCollaborative = servicePlaylist.IsCollaborative,
                    OwnerName = servicePlaylist.OwnerName,
                    ServiceUrl = servicePlaylist.ServiceUrl,
                    LastSyncedAt = syncedAt,
                    CreatedAt = syncedAt,
                    UpdatedAt = syncedAt
                };

                _context.ServicePlaylists.Add(newPlaylist);
                added++;
            }
        }

        // Remove playlists that no longer exist on the service
        var removed = existingPlaylistIds.Count;
        foreach (var removedPlaylist in existingPlaylistIds.Values)
        {
            _context.ServicePlaylists.Remove(removedPlaylist);
        }

        // Update connection's last synced time
        connection.LastSyncedAt = syncedAt;
        connection.UpdatedAt = syncedAt;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Playlist sync completed for connection {ConnectionId}: {Added} added, {Updated} updated, {Removed} removed",
            connectionId, added, updated, removed);

        return new SyncPlaylistsResponse
        {
            PlaylistsAdded = added,
            PlaylistsUpdated = updated,
            PlaylistsRemoved = removed,
            SyncedAt = syncedAt
        };
    }

    public async Task SyncAllUserPlaylistsAsync(Guid userId)
    {
        var connections = await _context.ServiceConnections
            .Where(sc => sc.UserId == userId && sc.ConnectionStatus == ConnectionStatus.Active)
            .ToListAsync();

        _logger.LogInformation("Syncing playlists for {Count} connections for user {UserId}",
            connections.Count, userId);

        foreach (var connection in connections)
        {
            try
            {
                await SyncPlaylistsAsync(connection.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to sync playlists for connection {ConnectionId}",
                    connection.Id);
            }
        }
    }

    public async Task SyncAllPlaylistsAsync()
    {
        var connections = await _context.ServiceConnections
            .Where(sc => sc.ConnectionStatus == ConnectionStatus.Active)
            .ToListAsync();

        _logger.LogInformation("Starting global playlist sync for {Count} connections", connections.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var connection in connections)
        {
            try
            {
                await SyncPlaylistsAsync(connection.Id);
                successCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex,
                    "Failed to sync playlists for connection {ConnectionId} (User: {UserId})",
                    connection.Id, connection.UserId);
            }
        }

        _logger.LogInformation(
            "Global playlist sync completed: {SuccessCount} succeeded, {FailureCount} failed",
            successCount, failureCount);
    }

    private ServicePlaylistDto MapToDto(ServicePlaylist playlist)
    {
        return new ServicePlaylistDto
        {
            Id = playlist.Id,
            ServiceConnectionId = playlist.ServiceConnectionId,
            ServiceType = playlist.ServiceConnection.ServiceType,
            ServicePlaylistId = playlist.ServicePlaylistId,
            Name = playlist.Name,
            Description = playlist.Description,
            ImageUrl = playlist.ImageUrl,
            TrackCount = playlist.TrackCount,
            IsPublic = playlist.IsPublic,
            IsCollaborative = playlist.IsCollaborative,
            OwnerName = playlist.OwnerName,
            ServiceUrl = playlist.ServiceUrl,
            LastSyncedAt = playlist.LastSyncedAt,
            CreatedAt = playlist.CreatedAt
        };
    }

    private PlaylistWithConnectionDto MapToPlaylistWithConnectionDto(ServicePlaylist playlist)
    {
        return new PlaylistWithConnectionDto
        {
            Id = playlist.Id,
            ServiceConnectionId = playlist.ServiceConnectionId,
            ServiceType = playlist.ServiceConnection.ServiceType,
            ServicePlaylistId = playlist.ServicePlaylistId,
            Name = playlist.Name,
            Description = playlist.Description,
            ImageUrl = playlist.ImageUrl,
            TrackCount = playlist.TrackCount,
            IsPublic = playlist.IsPublic,
            IsCollaborative = playlist.IsCollaborative,
            OwnerName = playlist.OwnerName,
            ServiceUrl = playlist.ServiceUrl,
            LastSyncedAt = playlist.LastSyncedAt,
            CreatedAt = playlist.CreatedAt,
            ServiceAccountDisplayName = playlist.ServiceConnection.ServiceAccountDisplayName,
            ServiceAccountEmail = playlist.ServiceConnection.ServiceAccountEmail
        };
    }
}

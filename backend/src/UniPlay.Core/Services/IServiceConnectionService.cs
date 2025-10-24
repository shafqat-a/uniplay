using UniPlay.Core.DTOs;
using UniPlay.Core.Enums;

namespace UniPlay.Core.Services;

/// <summary>
/// Service for managing user connections to music streaming services
/// </summary>
public interface IServiceConnectionService
{
    /// <summary>
    /// Initiate OAuth flow for a music service
    /// </summary>
    Task<OAuthUrlResponse> InitiateOAuthAsync(Guid userId, InitiateOAuthRequest request);

    /// <summary>
    /// Complete OAuth flow and create service connection
    /// </summary>
    Task<CompleteOAuthResponse> CompleteOAuthAsync(Guid userId, CompleteOAuthRequest request);

    /// <summary>
    /// Get all service connections for a user
    /// </summary>
    Task<List<ServiceConnectionDto>> GetUserConnectionsAsync(Guid userId);

    /// <summary>
    /// Get a specific service connection
    /// </summary>
    Task<ServiceConnectionDto?> GetConnectionAsync(Guid userId, Guid connectionId);

    /// <summary>
    /// Get all connections for a specific service type
    /// </summary>
    Task<List<ServiceConnectionDto>> GetConnectionsByServiceAsync(Guid userId, ServiceType serviceType);

    /// <summary>
    /// Disconnect a service (revoke and delete connection)
    /// </summary>
    Task DisconnectServiceAsync(Guid userId, Guid connectionId);

    /// <summary>
    /// Refresh access token for a connection
    /// </summary>
    Task RefreshConnectionTokenAsync(Guid connectionId);

    /// <summary>
    /// Check if user has reached connection limit for a service
    /// </summary>
    Task<bool> CanAddConnectionAsync(Guid userId, ServiceType serviceType);
}

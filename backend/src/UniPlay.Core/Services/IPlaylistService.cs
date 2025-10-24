using UniPlay.Core.DTOs;

namespace UniPlay.Core.Services;

/// <summary>
/// Service for managing playlists from music streaming services
/// </summary>
public interface IPlaylistService
{
    /// <summary>
    /// Get all playlists for a user across all services
    /// </summary>
    Task<List<PlaylistWithConnectionDto>> GetUserPlaylistsAsync(Guid userId);

    /// <summary>
    /// Get playlists for a specific service connection
    /// </summary>
    Task<List<ServicePlaylistDto>> GetConnectionPlaylistsAsync(Guid userId, Guid connectionId);

    /// <summary>
    /// Sync playlists from a music service
    /// </summary>
    Task<SyncPlaylistsResponse> SyncPlaylistsAsync(Guid connectionId);

    /// <summary>
    /// Sync playlists for all active connections of a user
    /// </summary>
    Task SyncAllUserPlaylistsAsync(Guid userId);

    /// <summary>
    /// Sync playlists for all active connections (background job)
    /// </summary>
    Task SyncAllPlaylistsAsync();
}

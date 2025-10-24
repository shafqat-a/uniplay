using UniPlay.Core.Enums;

namespace UniPlay.Core.DTOs;

/// <summary>
/// DTO for service playlist information
/// </summary>
public class ServicePlaylistDto
{
    public Guid Id { get; set; }
    public Guid ServiceConnectionId { get; set; }
    public ServiceType ServiceType { get; set; }
    public string ServicePlaylistId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int TrackCount { get; set; }
    public bool IsPublic { get; set; }
    public bool IsCollaborative { get; set; }
    public string? OwnerName { get; set; }
    public string? ServiceUrl { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for playlist with connection details
/// </summary>
public class PlaylistWithConnectionDto : ServicePlaylistDto
{
    public string? ServiceAccountDisplayName { get; set; }
    public string? ServiceAccountEmail { get; set; }
}

/// <summary>
/// Request to sync playlists for a connection
/// </summary>
public class SyncPlaylistsRequest
{
    public Guid ConnectionId { get; set; }
}

/// <summary>
/// Response after syncing playlists
/// </summary>
public class SyncPlaylistsResponse
{
    public int PlaylistsAdded { get; set; }
    public int PlaylistsUpdated { get; set; }
    public int PlaylistsRemoved { get; set; }
    public DateTime SyncedAt { get; set; }
}

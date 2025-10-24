namespace UniPlay.Core.Entities;

/// <summary>
/// Represents a playlist from a connected music service (Spotify, Apple Music, etc.)
/// Tracks metadata and sync status with external service.
/// </summary>
public class ServicePlaylist
{
    /// <summary>
    /// Unique platform identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// References ServiceConnection.Id
    /// </summary>
    public Guid ServiceConnectionId { get; set; }

    /// <summary>
    /// Playlist ID on the external service
    /// </summary>
    public string ServicePlaylistId { get; set; } = string.Empty;

    /// <summary>
    /// Playlist name from service
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Playlist description from service
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of tracks in the playlist
    /// </summary>
    public int TrackCount { get; set; }

    /// <summary>
    /// Cover image URL from service
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Playlist owner name from service
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Service URL to view playlist
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Whether user owns this playlist on the service
    /// </summary>
    public bool IsOwnedByUser { get; set; }

    /// <summary>
    /// Whether playlist is public on the service
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether playlist is collaborative on the service
    /// </summary>
    public bool IsCollaborative { get; set; }

    /// <summary>
    /// Snapshot ID from service (for change detection)
    /// </summary>
    public string? SnapshotId { get; set; }

    /// <summary>
    /// Last sync timestamp (UTC)
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ServiceConnection ServiceConnection { get; set; } = null!;
    public ICollection<PlaylistTrackAssociation> TrackAssociations { get; set; } = new List<PlaylistTrackAssociation>();
}

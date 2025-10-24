namespace UniPlay.Core.Entities;

/// <summary>
/// A playlist created within UniPlay that can contain tracks from any connected service.
/// Enables cross-service playlists managed by the platform.
/// </summary>
public class PlatformPlaylist
{
    /// <summary>
    /// Unique playlist identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// References UserAccount.Id (owner)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Playlist name (1-255 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// User-provided description (max 1000 characters)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Visibility (future: sharing feature)
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Denormalized track count for performance
    /// </summary>
    public int TrackCount { get; set; }

    /// <summary>
    /// Creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public UserAccount User { get; set; } = null!;
    public ICollection<PlaylistTrackAssociation> TrackAssociations { get; set; } = new List<PlaylistTrackAssociation>();
}

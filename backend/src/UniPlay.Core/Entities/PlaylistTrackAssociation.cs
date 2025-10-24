using UniPlay.Core.Enums;

namespace UniPlay.Core.Entities;

/// <summary>
/// Links tracks to playlists (both platform and service playlists) with ordering information.
/// Many-to-many relationship between playlists and tracks, maintains track order.
/// </summary>
public class PlaylistTrackAssociation
{
    /// <summary>
    /// Unique association identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// References PlatformPlaylist.Id or ServicePlaylist.Id
    /// </summary>
    public Guid PlaylistId { get; set; }

    /// <summary>
    /// Type of playlist (Platform or Service)
    /// </summary>
    public PlaylistType PlaylistType { get; set; }

    /// <summary>
    /// References Track.Id
    /// </summary>
    public Guid TrackId { get; set; }

    /// <summary>
    /// Zero-based position in playlist
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// When track was added (UTC)
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// References UserAccount.Id (for collaborative playlists - future feature)
    /// </summary>
    public Guid? AddedBy { get; set; }

    // Navigation properties
    public Track Track { get; set; } = null!;
    public PlatformPlaylist? PlatformPlaylist { get; set; }
    public ServicePlaylist? ServicePlaylist { get; set; }
}

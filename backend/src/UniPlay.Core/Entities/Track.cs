using UniPlay.Core.Enums;

namespace UniPlay.Core.Entities;

/// <summary>
/// Represents a music track with metadata from a specific service.
/// Stores track information for platform playlists and caching service track data.
/// </summary>
public class Track
{
    /// <summary>
    /// Unique internal track identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of music service (Spotify, AppleMusic, Deezer, YouTubeMusic)
    /// </summary>
    public ServiceType ServiceType { get; set; }

    /// <summary>
    /// Track ID from the music service
    /// </summary>
    public string ServiceTrackId { get; set; } = string.Empty;

    /// <summary>
    /// Track title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Primary artist name
    /// </summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>
    /// Album name
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Track duration in milliseconds
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// International Standard Recording Code (ISRC)
    /// Format: 2-letter country + 3-char registrant + 2-digit year + 5-digit designation
    /// </summary>
    public string? ISRC { get; set; }

    /// <summary>
    /// Album/track artwork URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Direct link to track on service
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Additional service-specific metadata (stored as JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// First cache timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<PlaylistTrackAssociation> PlaylistAssociations { get; set; } = new List<PlaylistTrackAssociation>();
}

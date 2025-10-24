using UniPlay.Core.Enums;

namespace UniPlay.Core.Entities;

/// <summary>
/// Links a user to a specific music service account (Spotify, YouTube Music, Apple Music, Deezer).
/// Stores OAuth credentials and connection status.
/// </summary>
public class ServiceConnection
{
    /// <summary>
    /// Unique connection identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// References UserAccount.Id
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of music service (Spotify, AppleMusic, Deezer, YouTubeMusic)
    /// </summary>
    public ServiceType ServiceType { get; set; }

    /// <summary>
    /// User's ID on the external service
    /// </summary>
    public string ServiceAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Account email (if available from service)
    /// </summary>
    public string? ServiceAccountEmail { get; set; }

    /// <summary>
    /// Display name from service
    /// </summary>
    public string? ServiceAccountDisplayName { get; set; }

    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ServiceAccountProfileImageUrl { get; set; }

    /// <summary>
    /// Encrypted OAuth access token
    /// </summary>
    public string EncryptedAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted OAuth refresh token (if available)
    /// </summary>
    public string? EncryptedRefreshToken { get; set; }

    /// <summary>
    /// Access token expiration timestamp (UTC)
    /// </summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>
    /// OAuth scopes granted (space-separated)
    /// </summary>
    public string Scopes { get; set; } = string.Empty;

    /// <summary>
    /// Connection status (Active, Expired, Revoked, Error)
    /// </summary>
    public ConnectionStatus ConnectionStatus { get; set; }

    /// <summary>
    /// Last successful sync timestamp (UTC)
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Connection creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public UserAccount User { get; set; } = null!;
    public ICollection<ServicePlaylist> ServicePlaylists { get; set; } = new List<ServicePlaylist>();
}

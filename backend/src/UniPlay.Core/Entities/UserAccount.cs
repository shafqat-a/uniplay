namespace UniPlay.Core.Entities;

/// <summary>
/// Represents a registered user of the UniPlay platform.
/// </summary>
public class UserAccount
{
    /// <summary>
    /// Unique user identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User email address (RFC 5321 max length)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Email verification status
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Hashed password (ASP.NET Core Identity)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Security stamp for token invalidation
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>
    /// Account creation timestamp (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Last successful login timestamp (UTC)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<ServiceConnection> ServiceConnections { get; set; } = new List<ServiceConnection>();
    public ICollection<PlatformPlaylist> PlatformPlaylists { get; set; } = new List<PlatformPlaylist>();
}

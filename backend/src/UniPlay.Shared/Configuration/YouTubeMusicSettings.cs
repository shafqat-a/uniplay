namespace UniPlay.Shared.Configuration;

/// <summary>
/// Configuration settings for YouTube Music integration
/// </summary>
public class YouTubeMusicSettings
{
    /// <summary>
    /// YouTube Music uses Google OAuth 2.0
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Google OAuth client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// OAuth redirect URI
    /// </summary>
    public string RedirectUri { get; set; } = "https://localhost:5001/api/v1/services/youtubemusic/callback";

    /// <summary>
    /// OAuth scopes required for YouTube Music access
    /// </summary>
    public string Scopes { get; set; } = "https://www.googleapis.com/auth/youtube https://www.googleapis.com/auth/youtube.readonly";

    /// <summary>
    /// YouTube Data API v3 key (required for playlist fetching)
    /// Note: Can be same project as OAuth credentials or separate
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}

namespace UniPlay.Shared.Configuration;

/// <summary>
/// Spotify OAuth configuration settings
/// </summary>
public class SpotifySettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Scopes required for Spotify integration
    /// </summary>
    public string[] Scopes { get; set; } = new[]
    {
        "user-read-private",
        "user-read-email",
        "playlist-read-private",
        "playlist-read-collaborative",
        "playlist-modify-public",
        "playlist-modify-private"
    };
}

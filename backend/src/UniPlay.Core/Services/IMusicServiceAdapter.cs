using UniPlay.Core.DTOs;
using UniPlay.Core.Entities;

namespace UniPlay.Core.Services;

/// <summary>
/// Interface for music service OAuth adapters (Spotify, Apple Music, etc.)
/// </summary>
public interface IMusicServiceAdapter
{
    /// <summary>
    /// Generate OAuth authorization URL for the service
    /// </summary>
    string GenerateAuthorizationUrl(string state, string redirectUri);

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    Task<OAuthTokenResult> ExchangeCodeForTokenAsync(string code, string redirectUri);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    Task<OAuthTokenResult> RefreshAccessTokenAsync(string refreshToken);

    /// <summary>
    /// Get user profile information from the service
    /// </summary>
    Task<ServiceUserProfile> GetUserProfileAsync(string accessToken);

    /// <summary>
    /// Revoke access token (disconnect service)
    /// </summary>
    Task RevokeTokenAsync(string accessToken);

    /// <summary>
    /// Fetch all playlists for the user from the service
    /// </summary>
    Task<List<ServicePlaylistInfo>> GetUserPlaylistsAsync(string accessToken);
}

/// <summary>
/// Result from OAuth token exchange
/// </summary>
public class OAuthTokenResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Scopes { get; set; } = string.Empty;
}

/// <summary>
/// User profile information from music service
/// </summary>
public class ServiceUserProfile
{
    public string ServiceAccountId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// Playlist information from music service
/// </summary>
public class ServicePlaylistInfo
{
    public string ServicePlaylistId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int TrackCount { get; set; }
    public bool IsPublic { get; set; }
    public bool IsCollaborative { get; set; }
    public string? OwnerName { get; set; }
    public string? ServiceUrl { get; set; }
}

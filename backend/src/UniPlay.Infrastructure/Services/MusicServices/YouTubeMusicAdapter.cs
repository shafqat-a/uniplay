using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UniPlay.Core.DTOs;
using UniPlay.Core.Services;
using UniPlay.Shared.Configuration;
using YouTubeMusicAPI;
using YouTubeMusicAPI.Models;

namespace UniPlay.Infrastructure.Services.MusicServices;

/// <summary>
/// YouTube Music service adapter implementation
/// Uses Google OAuth 2.0 for authentication
/// </summary>
public class YouTubeMusicAdapter : IMusicServiceAdapter
{
    private readonly YouTubeMusicSettings _settings;
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<YouTubeMusicAdapter> _logger;

    private const string GoogleOAuthAuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string GoogleOAuthTokenUrl = "https://oauth2.googleapis.com/token";
    private const string GoogleOAuthRevokeUrl = "https://oauth2.googleapis.com/revoke";

    public YouTubeMusicAdapter(
        IOptions<YouTubeMusicSettings> settings,
        IHttpClientService httpClient,
        ILogger<YouTubeMusicAdapter> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public string GenerateAuthorizationUrl(string state, string redirectUri)
    {
        var scopes = Uri.EscapeDataString(_settings.Scopes);
        var redirect = Uri.EscapeDataString(redirectUri);

        return $"{GoogleOAuthAuthUrl}" +
               $"?client_id={_settings.ClientId}" +
               $"&redirect_uri={redirect}" +
               $"&response_type=code" +
               $"&scope={scopes}" +
               $"&state={state}" +
               $"&access_type=offline" +
               $"&prompt=consent";
    }

    public async Task<OAuthTokenResult> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        _logger.LogInformation("Exchanging authorization code for access token");

        var requestBody = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code"
        };

        var content = new FormUrlEncodedContent(requestBody);
        var response = await _httpClient.PostAsync(GoogleOAuthTokenUrl, content);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize token response");
        }

        return new OAuthTokenResult
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            Scopes = _settings.Scopes
        };
    }

    public async Task<OAuthTokenResult> RefreshAccessTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Refreshing YouTube Music access token");

        var requestBody = new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["grant_type"] = "refresh_token"
        };

        var content = new FormUrlEncodedContent(requestBody);
        var response = await _httpClient.PostAsync(GoogleOAuthTokenUrl, content);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        if (tokenResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize token response");
        }

        return new OAuthTokenResult
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = refreshToken, // Google doesn't always return new refresh token
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
            Scopes = _settings.Scopes
        };
    }

    public async Task<ServiceUserProfile> GetUserProfileAsync(string accessToken)
    {
        _logger.LogInformation("Fetching YouTube Music user profile");

        // Google UserInfo endpoint with access token as query parameter
        var userInfoUrl = $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}";

        var response = await _httpClient.GetAsync(userInfoUrl);
        response.EnsureSuccessStatusCode();

        var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
        if (userInfo == null)
        {
            throw new InvalidOperationException("Failed to get user profile");
        }

        return new ServiceUserProfile
        {
            ServiceAccountId = userInfo.Id,
            Email = userInfo.Email,
            DisplayName = userInfo.Name ?? userInfo.Email,
            ProfileImageUrl = userInfo.Picture
        };
    }

    public async Task RevokeTokenAsync(string accessToken)
    {
        _logger.LogInformation("Revoking YouTube Music access token");

        try
        {
            var revokeUrl = $"{GoogleOAuthRevokeUrl}?token={accessToken}";
            var response = await _httpClient.PostAsync(revokeUrl, new StringContent(""));
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to revoke YouTube Music token");
            // Don't throw - token revocation is best effort
        }
    }

    public async Task<List<ServicePlaylistInfo>> GetUserPlaylistsAsync(string accessToken)
    {
        _logger.LogInformation("Fetching YouTube Music playlists via YouTube Data API v3");

        var playlists = new List<ServicePlaylistInfo>();

        try
        {
            // YouTube Data API v3 endpoint for fetching user's playlists
            // Note: API key is optional when using OAuth access token, but including it for quota tracking
            var apiKey = _settings.ApiKey;

            // Fetch playlists - using both mine=true and accessing with OAuth token
            var url = $"https://www.googleapis.com/youtube/v3/playlists" +
                     $"?part=snippet,contentDetails,status" +
                     $"&mine=true" +
                     $"&maxResults=50" +
                     $"&access_token={accessToken}";

            // Optionally add API key for quota tracking (not required with OAuth)
            if (!string.IsNullOrEmpty(apiKey))
            {
                url += $"&key={apiKey}";
            }

            string? nextPageToken = null;

            do
            {
                var requestUrl = nextPageToken != null
                    ? $"{url}&pageToken={nextPageToken}"
                    : url;

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("YouTube API error: {StatusCode} - {Content}",
                        response.StatusCode, errorContent);
                    break;
                }

                var data = await response.Content.ReadFromJsonAsync<YouTubePlaylistListResponse>();

                if (data?.Items == null || data.Items.Count == 0)
                {
                    break;
                }

                foreach (var item in data.Items)
                {
                    playlists.Add(new ServicePlaylistInfo
                    {
                        ServicePlaylistId = item.Id ?? Guid.NewGuid().ToString(),
                        Name = item.Snippet?.Title ?? "Untitled Playlist",
                        Description = item.Snippet?.Description,
                        ImageUrl = item.Snippet?.Thumbnails?.High?.Url
                                  ?? item.Snippet?.Thumbnails?.Default?.Url,
                        TrackCount = item.ContentDetails?.ItemCount ?? 0,
                        IsPublic = item.Status?.PrivacyStatus == "public",
                        IsCollaborative = false, // YouTube doesn't have collaborative playlists
                        OwnerName = item.Snippet?.ChannelTitle ?? "Unknown",
                        ServiceUrl = $"https://music.youtube.com/playlist?list={item.Id}"
                    });
                }

                nextPageToken = data.NextPageToken;

            } while (!string.IsNullOrEmpty(nextPageToken));

            _logger.LogInformation("Retrieved {Count} playlists from YouTube Music", playlists.Count);
            return playlists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching YouTube Music playlists");
            throw;
        }
    }

    // Helper classes for Google OAuth responses
    private class GoogleTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }

    private class GoogleUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Picture { get; set; }
    }

    // Helper classes for YouTube Data API v3 responses
    private class YouTubePlaylistListResponse
    {
        public string? NextPageToken { get; set; }
        public List<YouTubePlaylistItem> Items { get; set; } = new();
    }

    private class YouTubePlaylistItem
    {
        public string? Id { get; set; }
        public YouTubePlaylistSnippet? Snippet { get; set; }
        public YouTubePlaylistContentDetails? ContentDetails { get; set; }
        public YouTubePlaylistStatus? Status { get; set; }
    }

    private class YouTubePlaylistSnippet
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ChannelTitle { get; set; }
        public YouTubeThumbnails? Thumbnails { get; set; }
    }

    private class YouTubePlaylistContentDetails
    {
        public int ItemCount { get; set; }
    }

    private class YouTubePlaylistStatus
    {
        public string? PrivacyStatus { get; set; }
    }

    private class YouTubeThumbnails
    {
        public YouTubeThumbnail? Default { get; set; }
        public YouTubeThumbnail? Medium { get; set; }
        public YouTubeThumbnail? High { get; set; }
        public YouTubeThumbnail? Standard { get; set; }
        public YouTubeThumbnail? Maxres { get; set; }
    }

    private class YouTubeThumbnail
    {
        public string? Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}

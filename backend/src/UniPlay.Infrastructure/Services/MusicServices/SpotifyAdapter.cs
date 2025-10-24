using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using UniPlay.Core.Services;
using UniPlay.Shared.Configuration;

namespace UniPlay.Infrastructure.Services.MusicServices;

/// <summary>
/// Spotify OAuth adapter implementation using SpotifyAPI.Web
/// </summary>
public class SpotifyAdapter : IMusicServiceAdapter
{
    private readonly SpotifySettings _settings;
    private readonly ILogger<SpotifyAdapter> _logger;

    public SpotifyAdapter(
        IOptions<SpotifySettings> settings,
        ILogger<SpotifyAdapter> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public string GenerateAuthorizationUrl(string state, string redirectUri)
    {
        var loginRequest = new LoginRequest(
            new Uri(redirectUri),
            _settings.ClientId,
            LoginRequest.ResponseType.Code
        )
        {
            Scope = _settings.Scopes,
            State = state
        };

        var authorizationUrl = loginRequest.ToUri().ToString();
        _logger.LogInformation("Generated Spotify authorization URL with state: {State}", state);

        return authorizationUrl;
    }

    public async Task<OAuthTokenResult> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        try
        {
            var tokenRequest = new AuthorizationCodeTokenRequest(
                _settings.ClientId,
                _settings.ClientSecret,
                code,
                new Uri(redirectUri)
            );

            var oauthClient = new OAuthClient();
            var response = await oauthClient.RequestToken(tokenRequest);

            _logger.LogInformation("Successfully exchanged Spotify authorization code for access token");

            return new OAuthTokenResult
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn),
                Scopes = string.Join(",", _settings.Scopes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange Spotify authorization code for access token");
            throw;
        }
    }

    public async Task<OAuthTokenResult> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var refreshRequest = new AuthorizationCodeRefreshRequest(
                _settings.ClientId,
                _settings.ClientSecret,
                refreshToken
            );

            var oauthClient = new OAuthClient();
            var response = await oauthClient.RequestToken(refreshRequest);

            _logger.LogInformation("Successfully refreshed Spotify access token");

            return new OAuthTokenResult
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken ?? refreshToken, // Some responses don't include new refresh token
                ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn),
                Scopes = string.Join(",", _settings.Scopes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Spotify access token");
            throw;
        }
    }

    public async Task<ServiceUserProfile> GetUserProfileAsync(string accessToken)
    {
        try
        {
            var spotify = new SpotifyClient(accessToken);
            var profile = await spotify.UserProfile.Current();

            _logger.LogInformation("Retrieved Spotify user profile: {UserId}", profile.Id);

            return new ServiceUserProfile
            {
                ServiceAccountId = profile.Id,
                Email = profile.Email,
                DisplayName = profile.DisplayName,
                ProfileImageUrl = profile.Images?.FirstOrDefault()?.Url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Spotify user profile");
            throw;
        }
    }

    public async Task RevokeTokenAsync(string accessToken)
    {
        // Spotify doesn't have a token revocation endpoint in their API
        // Tokens are automatically revoked when user removes app access from Spotify account settings
        // We'll just log this action
        _logger.LogInformation("Spotify token revocation requested - tokens expire naturally");
        await Task.CompletedTask;
    }

    public async Task<List<ServicePlaylistInfo>> GetUserPlaylistsAsync(string accessToken)
    {
        try
        {
            var spotify = new SpotifyClient(accessToken);
            var playlists = new List<ServicePlaylistInfo>();

            // Fetch all user playlists (paginated)
            var firstPage = await spotify.Playlists.CurrentUsers();
            await foreach (var playlist in spotify.Paginate(firstPage))
            {
                playlists.Add(new ServicePlaylistInfo
                {
                    ServicePlaylistId = playlist.Id!,
                    Name = playlist.Name!,
                    Description = playlist.Description,
                    ImageUrl = playlist.Images?.FirstOrDefault()?.Url,
                    TrackCount = playlist.Tracks?.Total ?? 0,
                    IsPublic = playlist.Public ?? false,
                    IsCollaborative = playlist.Collaborative ?? false,
                    OwnerName = playlist.Owner?.DisplayName,
                    ServiceUrl = playlist.ExternalUrls?.FirstOrDefault().Value
                });
            }

            _logger.LogInformation("Retrieved {Count} playlists from Spotify", playlists.Count);
            return playlists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Spotify playlists");
            throw;
        }
    }
}

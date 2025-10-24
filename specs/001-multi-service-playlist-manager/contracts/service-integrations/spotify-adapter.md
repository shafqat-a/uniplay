# Spotify Adapter Contract

**Service**: Spotify Web API
**Library**: SpotifyAPI.Web v7.2.1
**Authentication**: OAuth 2.0 (Authorization Code Flow)
**Documentation**: https://developer.spotify.com/documentation/web-api

## OAuth Configuration

### Application Registration
- **Developer Console**: https://developer.spotify.com/dashboard
- **Redirect URI**: `https://app.uniplay.example.com/callback`
- **Scopes Required**:
  - `playlist-read-private`: Read user's private playlists
  - `playlist-read-collaborative`: Read collaborative playlists
  - `playlist-modify-public`: Modify public playlists
  - `playlist-modify-private`: Modify private playlists
  - `user-library-read`: Read saved tracks

### Token Lifecycle
- **Access Token Expiration**: 1 hour
- **Refresh Token**: Permanent (until revoked)
- **Auto-Refresh**: 5 minutes before expiration

## Adapter Interface

```csharp
public interface ISpotifyAdapter : IMusicServiceAdapter
{
    // OAuth
    Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);
    Task<ServiceConnectionResult> ExchangeCodeAsync(string code, string redirectUri);
    Task<ServiceConnectionResult> RefreshTokenAsync(string refreshToken);

    // Playlists
    Task<IEnumerable<ServicePlaylistDto>> GetUserPlaylistsAsync(string accessToken, int offset = 0, int limit = 50);
    Task<ServicePlaylistDetailDto> GetPlaylistAsync(string accessToken, string playlistId);
    Task<ServicePlaylistDto> CreatePlaylistAsync(string accessToken, string userId, string name, string description, bool isPublic);
    Task UpdatePlaylistAsync(string accessToken, string playlistId, string name, string description);
    Task DeletePlaylistAsync(string accessToken, string playlistId);

    // Tracks
    Task<IEnumerable<TrackDto>> GetPlaylistTracksAsync(string accessToken, string playlistId, int offset = 0, int limit = 100);
    Task AddTracksAsync(string accessToken, string playlistId, IEnumerable<string> trackUris, int? position = null);
    Task RemoveTracksAsync(string accessToken, string playlistId, IEnumerable<TrackPosition> tracks);
    Task ReorderTracksAsync(string accessToken, string playlistId, int rangeStart, int insertBefore, int rangeLength = 1);

    // Search
    Task<IEnumerable<TrackDto>> SearchTracksAsync(string accessToken, string query, int limit = 20);
    Task<TrackDto?> GetTrackAsync(string accessToken, string trackId);

    // User
    Task<UserProfileDto> GetCurrentUserAsync(string accessToken);
}
```

## Rate Limiting

### Limits
- **Rate Limit**: ~180 requests per minute (per access token)
- **Retry-After Header**: Respected when 429 returned
- **Backoff Strategy**: Exponential with jitter

### Implementation
```csharp
services.AddHttpClient<ISpotifyAdapter, SpotifyAdapter>()
    .AddResilienceHandler("spotify", builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<RateLimitException>()
        });

        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30)
        });
    });
```

## Data Mapping

### Playlist Mapping
```csharp
// Spotify API Response → ServicePlaylistDto
ServicePlaylistDto MapPlaylist(FullPlaylist spotifyPlaylist)
{
    return new ServicePlaylistDto
    {
        ServicePlaylistId = spotifyPlaylist.Id,
        Name = spotifyPlaylist.Name,
        Description = spotifyPlaylist.Description,
        ServiceOwnerId = spotifyPlaylist.Owner.Id,
        IsOwnedByUser = spotifyPlaylist.Owner.Id == currentUserId,
        TrackCount = spotifyPlaylist.Tracks.Total,
        ImageUrl = spotifyPlaylist.Images?.FirstOrDefault()?.Url,
        ServiceUrl = spotifyPlaylist.ExternalUrls["spotify"]
    };
}
```

### Track Mapping
```csharp
// Spotify API Response → TrackDto
TrackDto MapTrack(FullTrack spotifyTrack)
{
    return new TrackDto
    {
        ServiceType = ServiceType.Spotify,
        ServiceTrackId = spotifyTrack.Id,
        Title = spotifyTrack.Name,
        Artist = string.Join(", ", spotifyTrack.Artists.Select(a => a.Name)),
        Album = spotifyTrack.Album.Name,
        DurationMs = spotifyTrack.DurationMs,
        ISRC = spotifyTrack.ExternalIds.ContainsKey("isrc") ? spotifyTrack.ExternalIds["isrc"] : null,
        ImageUrl = spotifyTrack.Album.Images?.FirstOrDefault()?.Url,
        ServiceUrl = spotifyTrack.ExternalUrls["spotify"],
        Metadata = new
        {
            Popularity = spotifyTrack.Popularity,
            Explicit = spotifyTrack.Explicit,
            PreviewUrl = spotifyTrack.PreviewUrl
        }
    };
}
```

## Error Handling

### Spotify API Errors
| Status Code | Error | UniPlay Handling |
|-------------|-------|------------------|
| 401 | Unauthorized | Token expired → Auto-refresh → Retry |
| 403 | Forbidden | User lacks permissions → Mark connection as Error |
| 404 | Not Found | Playlist deleted → Update SyncStatus to Deleted |
| 429 | Rate Limit | Respect Retry-After → Queue for retry |
| 500, 502, 503 | Server Error | Exponential backoff → Retry up to 3 times |

### Example Implementation
```csharp
public async Task<ServicePlaylistDto> GetPlaylistAsync(string accessToken, string playlistId)
{
    try
    {
        var spotify = new SpotifyClient(accessToken);
        var playlist = await spotify.Playlists.Get(playlistId);
        return MapPlaylist(playlist);
    }
    catch (APIException ex) when (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
    {
        throw new TokenExpiredException("Spotify token expired", ex);
    }
    catch (APIException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
    {
        throw new PlaylistNotFoundException($"Spotify playlist {playlistId} not found", ex);
    }
    catch (APIException ex) when (ex.Response.StatusCode == (HttpStatusCode)429)
    {
        var retryAfter = ex.Response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(10);
        throw new RateLimitException($"Rate limited, retry after {retryAfter.TotalSeconds}s", retryAfter, ex);
    }
}
```

## Track Matching

### ISRC Matching (Preferred)
```csharp
public async Task<TrackDto?> FindTrackByISRCAsync(string accessToken, string isrc)
{
    var spotify = new SpotifyClient(accessToken);
    var searchRequest = new SearchRequest(SearchRequest.Types.Track, $"isrc:{isrc}");
    var results = await spotify.Search.Item(searchRequest);

    return results.Tracks.Items?.FirstOrDefault() != null
        ? MapTrack(results.Tracks.Items.First())
        : null;
}
```

### Fuzzy Metadata Matching
```csharp
public async Task<IEnumerable<TrackDto>> FindTracksByMetadataAsync(
    string accessToken,
    string title,
    string artist,
    string album = null)
{
    var query = $"track:{title} artist:{artist}";
    if (!string.IsNullOrEmpty(album))
        query += $" album:{album}";

    var spotify = new SpotifyClient(accessToken);
    var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
    var results = await spotify.Search.Item(searchRequest);

    return results.Tracks.Items?.Select(MapTrack) ?? Enumerable.Empty<TrackDto>();
}
```

## Pagination

Spotify uses offset-based pagination. Maximum 50 items per request for playlists, 100 for tracks.

```csharp
public async Task<IEnumerable<ServicePlaylistDto>> GetAllUserPlaylistsAsync(string accessToken)
{
    var allPlaylists = new List<ServicePlaylistDto>();
    var spotify = new SpotifyClient(accessToken);

    var offset = 0;
    const int limit = 50;

    while (true)
    {
        var page = await spotify.Playlists.CurrentUsers(new PlaylistCurrentUsersRequest
        {
            Limit = limit,
            Offset = offset
        });

        allPlaylists.AddRange(page.Items.Select(MapPlaylist));

        if (page.Next == null) break;
        offset += limit;
    }

    return allPlaylists;
}
```

## Testing

### Mock Spotify Responses
```csharp
[Fact]
public async Task GetPlaylist_ShouldMapSpotifyPlaylistCorrectly()
{
    // Arrange
    var mockSpotifyClient = new Mock<ISpotifyClient>();
    mockSpotifyClient.Setup(x => x.Playlists.Get(It.IsAny<string>()))
        .ReturnsAsync(new FullPlaylist
        {
            Id = "spotify123",
            Name = "Test Playlist",
            Description = "Test Description",
            Owner = new PublicUser { Id = "user123" },
            Tracks = new PlaylistTrackCollection { Total = 25 },
            Images = new List<Image> { new() { Url = "https://..." } }
        });

    var adapter = new SpotifyAdapter(mockSpotifyClient.Object);

    // Act
    var result = await adapter.GetPlaylistAsync("access_token", "spotify123");

    // Assert
    Assert.Equal("spotify123", result.ServicePlaylistId);
    Assert.Equal("Test Playlist", result.Name);
    Assert.Equal(25, result.TrackCount);
}
```

## Deployment Checklist

- [ ] Register application in Spotify Developer Dashboard
- [ ] Configure redirect URIs for all environments (dev, staging, prod)
- [ ] Store Client ID in appsettings.json
- [ ] Store Client Secret in Azure Key Vault / environment variables
- [ ] Configure rate limit policies in Polly
- [ ] Set up logging for Spotify API calls
- [ ] Test OAuth flow end-to-end
- [ ] Verify token refresh mechanism
- [ ] Test playlist operations (CRUD)
- [ ] Test track matching accuracy

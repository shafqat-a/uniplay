# Deezer Adapter Contract

**Service**: Deezer API
**Library**: AspNet.Security.OAuth.Deezer v9.4.0 (OAuth) + Custom HttpClient wrapper (API)
**Authentication**: OAuth 2.0 (Authorization Code Flow)
**Documentation**: https://developers.deezer.com/api

## OAuth Configuration

### Application Registration
- **Developer Portal**: https://developers.deezer.com/myapps
- **Redirect URI**: `https://app.uniplay.example.com/callback`
- **Permissions**: `basic_access`, `email`, `manage_library`, `delete_library`

### Token Lifecycle
- **Access Token Expiration**: Never expires (until revoked)
- **Refresh Token**: Not provided (access token is permanent)
- **Re-authorization**: Required if user revokes access

## Adapter Interface

```csharp
public interface IDeezerAdapter : IMusicServiceAdapter
{
    // OAuth
    string GetAuthorizationUrl(string redirectUri, string state, string[] permissions);
    Task<ServiceConnectionResult> ExchangeCodeAsync(string code);

    // Playlists
    Task<IEnumerable<ServicePlaylistDto>> GetUserPlaylistsAsync(string accessToken, int offset = 0, int limit = 25);
    Task<ServicePlaylistDetailDto> GetPlaylistAsync(string accessToken, string playlistId);
    Task<ServicePlaylistDto> CreatePlaylistAsync(string accessToken, string title, string description, bool isPublic);
    Task UpdatePlaylistAsync(string accessToken, string playlistId, string title, string description);
    Task DeletePlaylistAsync(string accessToken, string playlistId);

    // Tracks
    Task<IEnumerable<TrackDto>> GetPlaylistTracksAsync(string accessToken, string playlistId, int offset = 0, int limit = 100);
    Task<bool> AddTracksAsync(string accessToken, string playlistId, IEnumerable<string> trackIds);
    Task<bool> RemoveTracksAsync(string accessToken, string playlistId, IEnumerable<string> trackIds);
    Task ReorderTracksAsync(string accessToken, string playlistId, int[] trackOrder);

    // Search
    Task<IEnumerable<TrackDto>> SearchTracksAsync(string accessToken, string query, int limit = 25);
    Task<TrackDto?> GetTrackAsync(string accessToken, string trackId);

    // User
    Task<UserProfileDto> GetCurrentUserAsync(string accessToken);
}
```

## Rate Limiting

### Limits
- **Rate Limit**: 50 requests per 5 seconds per access token
- **Quota**: Variable based on application tier (default: fair use policy)
- **Throttling**: 429 Too Many Requests response when exceeded

### Implementation
```csharp
services.AddRateLimiter(options =>
{
    options.AddTokenBucketLimiter("deezer", opt =>
    {
        opt.TokenLimit = 50;
        opt.TokensPerPeriod = 50;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(5);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 100;
    });
});

services.AddHttpClient<IDeezerAdapter, DeezerAdapter>(client =>
{
    client.BaseAddress = new Uri("https://api.deezer.com/");
})
.AddResilienceHandler("deezer", builder =>
{
    builder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
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
ServicePlaylistDto MapPlaylist(DeezerPlaylist deezerPlaylist)
{
    return new ServicePlaylistDto
    {
        ServicePlaylistId = deezerPlaylist.Id.ToString(),
        Name = deezerPlaylist.Title,
        Description = deezerPlaylist.Description,
        ServiceOwnerId = deezerPlaylist.Creator.Id.ToString(),
        IsOwnedByUser = deezerPlaylist.IsLovable, // Deezer-specific field
        TrackCount = deezerPlaylist.NbTracks,
        ImageUrl = deezerPlaylist.PictureXl ?? deezerPlaylist.Picture,
        ServiceUrl = deezerPlaylist.Link
    };
}
```

### Track Mapping
```csharp
TrackDto MapTrack(DeezerTrack deezerTrack)
{
    return new TrackDto
    {
        ServiceType = ServiceType.Deezer,
        ServiceTrackId = deezerTrack.Id.ToString(),
        Title = deezerTrack.Title,
        Artist = deezerTrack.Artist?.Name ?? "Unknown Artist",
        Album = deezerTrack.Album?.Title,
        DurationMs = deezerTrack.Duration * 1000, // Deezer returns seconds
        ISRC = deezerTrack.Isrc,
        ImageUrl = deezerTrack.Album?.CoverXl ?? deezerTrack.Album?.Cover,
        ServiceUrl = deezerTrack.Link,
        Metadata = new
        {
            Rank = deezerTrack.Rank,
            Explicit = deezerTrack.ExplicitLyrics,
            Preview = deezerTrack.Preview
        }
    };
}
```

## Error Handling

### Deezer API Errors
| Error Type | HTTP Status | UniPlay Handling |
|------------|-------------|------------------|
| `INVALID_TOKEN` | 401 | Token revoked → Prompt re-authorization |
| `QUOTA_EXCEEDED` | 403 | Rate limit → Exponential backoff |
| `DATA_NOT_FOUND` | 404 | Playlist deleted → Update SyncStatus |
| `INDIVIDUAL_ACCOUNT_REQUIRED` | 403 | User not subscribed → Mark connection as Error |
| `SERVICE_BUSY` | 503 | Temporary outage → Retry with backoff |

### Example Implementation
```csharp
public async Task<ServicePlaylistDto> GetPlaylistAsync(string accessToken, string playlistId)
{
    var url = $"playlist/{playlistId}?access_token={accessToken}";

    try
    {
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var error = JsonSerializer.Deserialize<DeezerErrorResponse>(content);

            return error.Error.Type switch
            {
                "INVALID_TOKEN" => throw new TokenExpiredException("Deezer token invalid or revoked"),
                "DATA_NOT_FOUND" => throw new PlaylistNotFoundException($"Deezer playlist {playlistId} not found"),
                "QUOTA_EXCEEDED" => throw new RateLimitException("Deezer rate limit exceeded", TimeSpan.FromSeconds(5)),
                _ => throw new MusicServiceException($"Deezer API error: {error.Error.Message}")
            };
        }

        var playlist = JsonSerializer.Deserialize<DeezerPlaylist>(content);
        return MapPlaylist(playlist);
    }
    catch (HttpRequestException ex)
    {
        throw new MusicServiceException("Deezer API connection error", ex);
    }
}
```

## Track Matching

### ISRC Matching
```csharp
public async Task<TrackDto?> FindTrackByISRCAsync(string accessToken, string isrc)
{
    var url = $"track/isrc:{isrc}?access_token={accessToken}";

    var response = await _httpClient.GetAsync(url);

    if (!response.IsSuccessStatusCode)
        return null;

    var content = await response.Content.ReadAsStringAsync();
    var track = JsonSerializer.Deserialize<DeezerTrack>(content);

    return MapTrack(track);
}
```

### Metadata Search
```csharp
public async Task<IEnumerable<TrackDto>> SearchTracksAsync(string accessToken, string query, int limit = 25)
{
    var encodedQuery = Uri.EscapeDataString(query);
    var url = $"search/track?q={encodedQuery}&limit={limit}&access_token={accessToken}";

    var response = await _httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadAsStringAsync();
    var searchResult = JsonSerializer.Deserialize<DeezerSearchResponse>(content);

    return searchResult.Data?.Select(MapTrack) ?? Enumerable.Empty<TrackDto>();
}
```

## OAuth Flow Implementation

### Using AspNet.Security.OAuth.Deezer

```csharp
// Startup.cs
services.AddAuthentication()
    .AddDeezer(options =>
    {
        options.ClientId = configuration["Deezer:ClientId"];
        options.ClientSecret = configuration["Deezer:ClientSecret"];
        options.CallbackPath = "/signin-deezer";

        options.Scope.Add("basic_access");
        options.Scope.Add("email");
        options.Scope.Add("manage_library");
        options.Scope.Add("delete_library");

        options.SaveTokens = true;

        options.Events.OnCreatingTicket = async context =>
        {
            // Get user profile
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.deezer.com/user/me?access_token={context.AccessToken}");

            var response = await context.Backchannel.SendAsync(request);
            var user = await response.Content.ReadFromJsonAsync<DeezerUser>();

            context.Identity.AddClaim(new Claim("deezer_user_id", user.Id.ToString()));
        };
    });
```

### Custom Authorization URL
```csharp
public string GetAuthorizationUrl(string redirectUri, string state, string[] permissions)
{
    var perms = string.Join(",", permissions);
    return $"https://connect.deezer.com/oauth/auth.php" +
           $"?app_id={_clientId}" +
           $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
           $"&perms={perms}" +
           $"&state={state}";
}
```

## Playlist Operations

### Create Playlist
```csharp
public async Task<ServicePlaylistDto> CreatePlaylistAsync(
    string accessToken,
    string title,
    string description,
    bool isPublic)
{
    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("title", title),
        new KeyValuePair<string, string>("description", description ?? ""),
        new KeyValuePair<string, string>("public", isPublic.ToString().ToLower()),
        new KeyValuePair<string, string>("access_token", accessToken)
    });

    var response = await _httpClient.PostAsync("user/me/playlists", content);
    var result = await response.Content.ReadAsStringAsync();
    var playlistId = JsonSerializer.Deserialize<DeezerCreateResponse>(result).Id;

    return await GetPlaylistAsync(accessToken, playlistId.ToString());
}
```

### Add Tracks to Playlist
```csharp
public async Task<bool> AddTracksAsync(string accessToken, string playlistId, IEnumerable<string> trackIds)
{
    var tracks = string.Join(",", trackIds);
    var url = $"playlist/{playlistId}/tracks?songs={tracks}&access_token={accessToken}";

    var response = await _httpClient.PostAsync(url, null);
    return response.IsSuccessStatusCode;
}
```

### Remove Tracks
```csharp
public async Task<bool> RemoveTracksAsync(string accessToken, string playlistId, IEnumerable<string> trackIds)
{
    var tracks = string.Join(",", trackIds);
    var url = $"playlist/{playlistId}/tracks?songs={tracks}&access_token={accessToken}";

    var request = new HttpRequestMessage(HttpMethod.Delete, url);
    var response = await _httpClient.SendAsync(request);

    return response.IsSuccessStatusCode;
}
```

### Reorder Tracks
```csharp
public async Task ReorderTracksAsync(string accessToken, string playlistId, int[] trackOrder)
{
    var order = string.Join(",", trackOrder);
    var url = $"playlist/{playlistId}/tracks?order={order}&access_token={accessToken}";

    var response = await _httpClient.PostAsync(url, null);
    response.EnsureSuccessStatusCode();
}
```

## Pagination

Deezer uses index-based pagination with a maximum of 100 items per request.

```csharp
public async Task<IEnumerable<ServicePlaylistDto>> GetAllUserPlaylistsAsync(string accessToken)
{
    var allPlaylists = new List<ServicePlaylistDto>();
    var index = 0;
    const int limit = 25;

    while (true)
    {
        var url = $"user/me/playlists?index={index}&limit={limit}&access_token={accessToken}";
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DeezerPagedResponse<DeezerPlaylist>>(content);

        if (result.Data == null || !result.Data.Any())
            break;

        allPlaylists.AddRange(result.Data.Select(MapPlaylist));

        if (!result.Next) break;
        index += limit;
    }

    return allPlaylists;
}
```

## Testing

### Mock Deezer Responses
```csharp
[Fact]
public async Task GetPlaylist_ShouldParseDeezerPlaylist()
{
    // Arrange
    var mockHttp = new Mock<HttpMessageHandler>();
    mockHttp.Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(@"{
                ""id"": 12345,
                ""title"": ""Test Playlist"",
                ""description"": ""Test"",
                ""nb_tracks"": 20,
                ""creator"": { ""id"": 987 },
                ""link"": ""https://www.deezer.com/playlist/12345""
            }")
        });

    var httpClient = new HttpClient(mockHttp.Object)
    {
        BaseAddress = new Uri("https://api.deezer.com/")
    };

    var adapter = new DeezerAdapter(httpClient, /* other dependencies */);

    // Act
    var result = await adapter.GetPlaylistAsync("access_token", "12345");

    // Assert
    Assert.Equal("12345", result.ServicePlaylistId);
    Assert.Equal("Test Playlist", result.Name);
    Assert.Equal(20, result.TrackCount);
}
```

## Deployment Checklist

- [ ] Register application at https://developers.deezer.com/myapps
- [ ] Configure redirect URIs for all environments
- [ ] Store App ID and Secret Key in Azure Key Vault
- [ ] Install AspNet.Security.OAuth.Deezer NuGet package
- [ ] Configure OAuth middleware in Startup.cs
- [ ] Implement rate limiting (50 req/5sec)
- [ ] Test OAuth flow end-to-end
- [ ] Test playlist operations (CRUD)
- [ ] Test track matching with ISRC
- [ ] Configure error handling for Deezer-specific errors

## Limitations

- **No Token Refresh**: Access tokens never expire but cannot be refreshed (require re-authorization)
- **Rate Limiting**: Strict 50 requests per 5 seconds limit
- **Limited Batch Operations**: Maximum 1000 tracks per add/remove operation
- **Subscription Tiers**: Some features require Deezer Premium subscription
- **Regional Availability**: Service not available in all countries

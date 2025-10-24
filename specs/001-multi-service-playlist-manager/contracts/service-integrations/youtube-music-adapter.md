# YouTube Music Adapter Contract

**Service**: YouTube Data API v3 + YouTube Music (unofficial)
**Library**: Google.Apis.YouTube.v3 + Custom wrapper
**Authentication**: OAuth 2.0 (Google OAuth)
**Documentation**: https://developers.google.com/youtube/v3/docs

⚠️ **IMPORTANT LIMITATION**: YouTube Music does not have an official public API. This adapter uses the YouTube Data API v3 which has limited music-specific features.

## Current Status

**Recommendation**: **Start without YouTube Music support**. Add later if demand justifies the development effort.

### Challenges

1. **No Official API**: YouTube Music has no official API for third-party playlist management
2. **YouTube Data API Limitations**:
   - Designed for videos, not music tracks
   - No music-specific metadata (artist, album, ISRC)
   - Playlist operations work but track matching is problematic
3. **Unofficial APIs**: Exist (ytmusicapi, YouTube Music API) but:
   - Use cookie-based authentication (not suitable for multi-user apps)
   - Violate YouTube Terms of Service
   - Unreliable and subject to breakage

## Adapter Interface (YouTube Data API v3)

```csharp
public interface IYouTubeMusicAdapter : IMusicServiceAdapter
{
    // OAuth
    Task<string> GetAuthorizationUrlAsync(string redirectUri, string state);
    Task<ServiceConnectionResult> ExchangeCodeAsync(string code, string redirectUri);
    Task<ServiceConnectionResult> RefreshTokenAsync(string refreshToken);

    // Playlists (YouTube Data API)
    Task<IEnumerable<ServicePlaylistDto>> GetUserPlaylistsAsync(string accessToken, int maxResults = 50);
    Task<ServicePlaylistDetailDto> GetPlaylistAsync(string accessToken, string playlistId);
    Task<ServicePlaylistDto> CreatePlaylistAsync(string accessToken, string title, string description, bool isPublic);
    Task UpdatePlaylistAsync(string accessToken, string playlistId, string title, string description);
    Task DeletePlaylistAsync(string accessToken, string playlistId);

    // Playlist Items
    Task<IEnumerable<TrackDto>> GetPlaylistItemsAsync(string accessToken, string playlistId);
    Task AddItemAsync(string accessToken, string playlistId, string videoId, int? position = null);
    Task RemoveItemAsync(string accessToken, string playlistItemId);

    // Search (Video-based)
    Task<IEnumerable<TrackDto>> SearchVideosAsync(string accessToken, string query, int maxResults = 25);
}
```

## OAuth Configuration

### Application Registration
- **Google Cloud Console**: https://console.cloud.google.com/
- **Enable APIs**: YouTube Data API v3
- **OAuth Consent Screen**: Configure application name, logo, privacy policy
- **Redirect URI**: `https://app.uniplay.example.com/callback`
- **Scopes Required**:
  - `https://www.googleapis.com/auth/youtube`: Full YouTube access
  - `https://www.googleapis.com/auth/youtube.readonly`: Read-only access

### Token Lifecycle
- **Access Token Expiration**: 1 hour
- **Refresh Token**: Permanent (until revoked)
- **Auto-Refresh**: 5 minutes before expiration

## Implementation (Using Google.Apis.YouTube.v3)

```csharp
services.AddScoped<IYouTubeMusicAdapter, YouTubeMusicAdapter>();

// Configure Google OAuth
services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Google:ClientId"];
        options.ClientSecret = configuration["Google:ClientSecret"];
        options.CallbackPath = "/signin-google";

        options.Scope.Add("https://www.googleapis.com/auth/youtube");
        options.SaveTokens = true;
    });

// YouTube Data API client
services.AddHttpClient<YouTubeService>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
});
```

## Rate Limiting

### YouTube Data API Quota
- **Daily Quota**: 10,000 units per day (default)
- **Cost per Operation**:
  - List playlists: 1 unit
  - Get playlist details: 1 unit
  - Insert playlist item: 50 units
  - Delete playlist item: 50 units
  - Search: 100 units

**Problem**: Default quota is insufficient for production use. Copy operation with 100 tracks = 5,000 units (half daily quota).

### Quota Request
- Can request quota increase via Google Cloud Console
- Higher quotas require justification and review
- Approval not guaranteed

## Data Mapping (YouTube Video as Track)

### "Track" Mapping (Actually YouTube Video)
```csharp
TrackDto MapYouTubeVideo(Video video)
{
    return new TrackDto
    {
        ServiceType = ServiceType.YouTubeMusic,
        ServiceTrackId = video.Id,
        Title = video.Snippet.Title,
        Artist = video.Snippet.ChannelTitle, // Channel name, not music artist!
        Album = null, // Not available
        DurationMs = ParseDuration(video.ContentDetails.Duration), // ISO 8601 duration
        ISRC = null, // Not available in YouTube Data API
        ImageUrl = video.Snippet.Thumbnails?.High?.Url,
        ServiceUrl = $"https://music.youtube.com/watch?v={video.Id}",
        Metadata = new
        {
            ViewCount = video.Statistics?.ViewCount,
            ChannelId = video.Snippet.ChannelId,
            CategoryId = video.Snippet.CategoryId
        }
    };
}

private int ParseDuration(string iso8601Duration)
{
    // Example: "PT4M33S" → 273000ms
    var duration = System.Xml.XmlConvert.ToTimeSpan(iso8601Duration);
    return (int)duration.TotalMilliseconds;
}
```

### Problem with Track Matching

**YouTube videos are not music tracks:**
- No standardized artist/album metadata
- Channel name ≠ Artist name
- No ISRC codes
- Same song may have multiple uploads
- Track matching accuracy: **30-50%** (far below 85% target)

## Playlist Operations

### Get User Playlists
```csharp
public async Task<IEnumerable<ServicePlaylistDto>> GetUserPlaylistsAsync(string accessToken, int maxResults = 50)
{
    var youtubeService = new YouTubeService(new BaseClientService.Initializer
    {
        ApiKey = _apiKey,
        ApplicationName = "UniPlay"
    });

    var request = youtubeService.Playlists.List("snippet,contentDetails");
    request.Mine = true;
    request.MaxResults = maxResults;
    request.OauthToken = accessToken;

    var response = await request.ExecuteAsync();

    return response.Items.Select(playlist => new ServicePlaylistDto
    {
        ServicePlaylistId = playlist.Id,
        Name = playlist.Snippet.Title,
        Description = playlist.Snippet.Description,
        TrackCount = (int)(playlist.ContentDetails?.ItemCount ?? 0),
        ImageUrl = playlist.Snippet.Thumbnails?.High?.Url,
        ServiceUrl = $"https://music.youtube.com/playlist?list={playlist.Id}"
    });
}
```

### Add Item to Playlist
```csharp
public async Task AddItemAsync(string accessToken, string playlistId, string videoId, int? position = null)
{
    var youtubeService = new YouTubeService(new BaseClientService.Initializer
    {
        ApplicationName = "UniPlay"
    });

    var playlistItem = new PlaylistItem
    {
        Snippet = new PlaylistItemSnippet
        {
            PlaylistId = playlistId,
            ResourceId = new ResourceId
            {
                Kind = "youtube#video",
                VideoId = videoId
            },
            Position = position
        }
    };

    var request = youtubeService.PlaylistItems.Insert(playlistItem, "snippet");
    request.OauthToken = accessToken;

    await request.ExecuteAsync(); // Costs 50 quota units!
}
```

## Alternative: Skip YouTube Music (Recommended)

Given the limitations, the recommended approach is:

1. **Phase 1**: Launch without YouTube Music support
2. **Monitor**: Track user requests for YouTube Music integration
3. **Re-evaluate**: If demand is high (>30% of users request it), consider:
   - Building unofficial API wrapper (high risk, TOS violation)
   - Waiting for official YouTube Music API
   - Partnering with YouTube for API access

## If Implementing Despite Limitations

### Workaround for Track Matching

```csharp
public async Task<IEnumerable<TrackDto>> SearchVideosAsync(string accessToken, string query, int maxResults = 25)
{
    var youtubeService = new YouTubeService(/* ... */);

    var searchRequest = youtubeService.Search.List("snippet");
    searchRequest.Q = query + " official audio"; // Improve music relevance
    searchRequest.Type = "video";
    searchRequest.VideoCategoryId = "10"; // Music category
    searchRequest.MaxResults = maxResults;
    searchRequest.OauthToken = accessToken;

    var searchResponse = await searchRequest.ExecuteAsync();

    // Get video details for duration (requires additional API call)
    var videoIds = string.Join(",", searchResponse.Items.Select(item => item.Id.VideoId));
    var videoRequest = youtubeService.Videos.List("snippet,contentDetails,statistics");
    videoRequest.Id = videoIds;

    var videoResponse = await videoRequest.ExecuteAsync();

    return videoResponse.Items.Select(MapYouTubeVideo);
}
```

### Fuzzy Matching Strategy
```csharp
// Example: Match "Bohemian Rhapsody - Queen"
public async Task<TrackDto?> FindTrackByMetadataAsync(
    string accessToken,
    string title,
    string artist)
{
    var query = $"{artist} {title} official audio";
    var results = await SearchVideosAsync(accessToken, query, 5);

    // Use FuzzySharp to find best match
    var bestMatch = results
        .Select(track => new
        {
            Track = track,
            Score = Fuzz.TokenSetRatio($"{artist} {title}", track.Title)
        })
        .OrderByDescending(x => x.Score)
        .FirstOrDefault();

    // Only return if confidence is medium+ (70%)
    return bestMatch?.Score >= 70 ? bestMatch.Track : null;
}
```

## Error Handling

```csharp
public async Task<ServicePlaylistDto> GetPlaylistAsync(string accessToken, string playlistId)
{
    try
    {
        var youtubeService = new YouTubeService(/* ... */);
        var request = youtubeService.Playlists.List("snippet,contentDetails");
        request.Id = playlistId;
        request.OauthToken = accessToken;

        var response = await request.ExecuteAsync();

        if (!response.Items.Any())
            throw new PlaylistNotFoundException($"YouTube playlist {playlistId} not found");

        return MapPlaylist(response.Items.First());
    }
    catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
    {
        throw new TokenExpiredException("YouTube token expired", ex);
    }
    catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Forbidden)
    {
        if (ex.Message.Contains("quota"))
            throw new QuotaExceededException("YouTube API quota exceeded", ex);

        throw new ServiceAuthorizationException("YouTube access forbidden", ex);
    }
}
```

## Testing

```csharp
[Fact]
public void MapYouTubeVideo_ShouldHandleMusicVideo()
{
    // Arrange
    var video = new Video
    {
        Id = "youtube123",
        Snippet = new VideoSnippet
        {
            Title = "Queen - Bohemian Rhapsody (Official Video)",
            ChannelTitle = "Queen Official",
            Thumbnails = new ThumbnailDetails
            {
                High = new Thumbnail { Url = "https://..." }
            }
        },
        ContentDetails = new VideoContentDetails
        {
            Duration = "PT5M55S"
        }
    };

    var adapter = new YouTubeMusicAdapter(/* dependencies */);

    // Act
    var result = adapter.MapYouTubeVideo(video);

    // Assert
    Assert.Equal("youtube123", result.ServiceTrackId);
    Assert.Equal(355000, result.DurationMs); // 5:55
    Assert.Null(result.ISRC); // Not available
}
```

## Deployment Checklist (If Implementing)

- [ ] Create project in Google Cloud Console
- [ ] Enable YouTube Data API v3
- [ ] Configure OAuth consent screen
- [ ] Generate OAuth 2.0 credentials
- [ ] Store Client ID and Secret in Azure Key Vault
- [ ] Request quota increase (justify with usage estimates)
- [ ] Install Google.Apis.YouTube.v3 NuGet package
- [ ] Implement quota monitoring and alerts
- [ ] Document limitations to users (video-based, not music-specific)
- [ ] Set user expectations for track matching accuracy

## Recommendation Summary

**Do NOT implement YouTube Music in MVP**:

1. **Technical Limitations**:
   - No music-specific metadata (artist, album, ISRC)
   - Track matching accuracy: 30-50% (fails SC-004: 85% target)
   - API quota constraints (10,000 units/day insufficient)

2. **Alternative Approach**:
   - Launch with Spotify, Apple Music, Deezer (all have proper music APIs)
   - Add "YouTube Music Coming Soon" placeholder in UI
   - Monitor user feedback and feature requests
   - Re-evaluate when/if YouTube releases official Music API

3. **If Users Demand It**:
   - Clearly document limitations (video-based, not music)
   - Require explicit user acknowledgment
   - Implement as beta/experimental feature
   - Focus on user-created playlists (owned playlists only)

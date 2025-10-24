# Background Jobs Contract

**Framework**: Hangfire
**Storage**: SQL Server / PostgreSQL
**Documentation**: https://docs.hangfire.io/

## Overview

Hangfire manages all long-running and asynchronous operations for the UniPlay platform:

- **Sync Operations**: Synchronize service playlist changes back to music services
- **Copy Operations**: Cross-service playlist copying with track matching
- **Export Operations**: Export platform playlists to music services
- **Token Refresh**: Automatic OAuth token refresh before expiration
- **Periodic Sync**: Scheduled playlist sync from connected services

## Job Types

### 1. Sync Queue Processor

Processes queued playlist edit operations and syncs them to music services.

**Trigger**: Continuous (polls SyncQueue table)
**Frequency**: Every 10 seconds
**Priority**: High (Priority 1-3 items processed first)

```csharp
public interface ISyncQueueProcessor
{
    Task ProcessNextBatchAsync(int batchSize = 10);
    Task ProcessQueueItemAsync(Guid queueItemId);
}

[AutomaticRetry(Attempts = 0)] // Manual retry via SyncQueue.RetryCount
public class SyncQueueProcessor : ISyncQueueProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncQueueProcessor> _logger;

    public async Task ProcessNextBatchAsync(int batchSize = 10)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pendingItems = await dbContext.SyncQueue
            .Where(sq => sq.Status == SyncQueueStatus.Pending &&
                        (sq.NextRetryAt == null || sq.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(sq => sq.Priority)
            .ThenBy(sq => sq.CreatedAt)
            .Take(batchSize)
            .ToListAsync();

        foreach (var item in pendingItems)
        {
            BackgroundJob.Enqueue(() => ProcessQueueItemAsync(item.Id));
        }
    }

    public async Task ProcessQueueItemAsync(Guid queueItemId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var queueItem = await dbContext.SyncQueue
            .Include(sq => sq.ServiceConnection)
            .Include(sq => sq.ServicePlaylist)
            .FirstOrDefaultAsync(sq => sq.Id == queueItemId);

        if (queueItem == null || queueItem.Status != SyncQueueStatus.Pending)
            return;

        queueItem.Status = SyncQueueStatus.Processing;
        await dbContext.SaveChangesAsync();

        try
        {
            var adapter = GetAdapterForService(queueItem.ServiceConnection.ServiceType);

            switch (queueItem.OperationType)
            {
                case SyncOperationType.AddTrack:
                    await ProcessAddTrackAsync(adapter, queueItem);
                    break;
                case SyncOperationType.RemoveTrack:
                    await ProcessRemoveTrackAsync(adapter, queueItem);
                    break;
                case SyncOperationType.ReorderTrack:
                    await ProcessReorderTrackAsync(adapter, queueItem);
                    break;
                case SyncOperationType.RenamePlaylist:
                    await ProcessRenamePlaylistAsync(adapter, queueItem);
                    break;
                case SyncOperationType.DeletePlaylist:
                    await ProcessDeletePlaylistAsync(adapter, queueItem);
                    break;
            }

            queueItem.Status = SyncQueueStatus.Completed;
            queueItem.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync queue item {QueueItemId} failed", queueItemId);

            queueItem.RetryCount++;
            queueItem.ErrorMessage = ex.Message;

            if (queueItem.RetryCount >= queueItem.MaxRetries)
            {
                queueItem.Status = SyncQueueStatus.Failed;
            }
            else
            {
                queueItem.Status = SyncQueueStatus.Pending;
                queueItem.NextRetryAt = DateTime.UtcNow.AddSeconds(
                    Math.Min(300, Math.Pow(2, queueItem.RetryCount)) // Exponential backoff, max 5min
                );
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
```

**Hangfire Configuration**:
```csharp
RecurringJob.AddOrUpdate<ISyncQueueProcessor>(
    "sync-queue-processor",
    x => x.ProcessNextBatchAsync(10),
    "*/10 * * * * *" // Every 10 seconds
);
```

---

### 2. Playlist Copy Job

Handles cross-service playlist copying with track matching.

**Trigger**: User-initiated via `/playlist-operations/copy` endpoint
**Duration**: 1-20 minutes (depends on playlist size)
**Progress Tracking**: SignalR real-time updates

```csharp
public interface IPlaylistCopyJob
{
    Task ExecuteAsync(Guid copyOperationId);
}

public class PlaylistCopyJob : IPlaylistCopyJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<PlaylistHub> _hubContext;
    private readonly ITrackMatchingService _trackMatchingService;
    private readonly ILogger<PlaylistCopyJob> _logger;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync(Guid copyOperationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var operation = await dbContext.CopyOperations
            .Include(co => co.SourceServiceConnection)
            .Include(co => co.DestinationServiceConnection)
            .Include(co => co.SourcePlaylist)
            .FirstOrDefaultAsync(co => co.Id == copyOperationId);

        if (operation == null || operation.Status != CopyOperationStatus.Pending)
            return;

        try
        {
            // Step 1: Get source playlist tracks
            operation.Status = CopyOperationStatus.Matching;
            await UpdateProgressAsync(operation, 0);

            var sourceAdapter = GetAdapterForService(operation.SourceServiceConnection.ServiceType);
            var sourceTracks = await sourceAdapter.GetPlaylistTracksAsync(
                operation.SourceServiceConnection.EncryptedAccessToken,
                operation.SourcePlaylist.ServicePlaylistId
            );

            operation.TotalTracks = sourceTracks.Count();
            await dbContext.SaveChangesAsync();

            // Step 2: Match tracks on destination service
            var destinationAdapter = GetAdapterForService(operation.DestinationServiceConnection.ServiceType);
            var matchResults = new List<TrackMatchResult>();

            int processed = 0;
            foreach (var sourceTrack in sourceTracks)
            {
                var matchResult = await _trackMatchingService.FindMatchAsync(
                    sourceTrack,
                    operation.DestinationServiceConnection
                );

                matchResults.Add(matchResult);
                processed++;

                operation.ProcessedTracks = processed;
                operation.MatchedTracks = matchResults.Count(r => r.IsMatched);
                operation.UnmatchedTracks = matchResults.Count(r => !r.IsMatched);

                await UpdateProgressAsync(operation, (int)((processed / (double)operation.TotalTracks) * 80));
            }

            // Step 3: Create destination playlist
            operation.Status = CopyOperationStatus.Creating;
            await UpdateProgressAsync(operation, 85);

            var destinationPlaylist = await destinationAdapter.CreatePlaylistAsync(
                operation.DestinationServiceConnection.EncryptedAccessToken,
                operation.SourcePlaylist.Name,
                operation.SourcePlaylist.Description,
                isPublic: false
            );

            // Step 4: Add matched tracks
            var matchedTrackIds = matchResults
                .Where(r => r.IsMatched)
                .Select(r => r.DestinationTrackId)
                .ToList();

            if (matchedTrackIds.Any())
            {
                await destinationAdapter.AddTracksAsync(
                    operation.DestinationServiceConnection.EncryptedAccessToken,
                    destinationPlaylist.ServicePlaylistId,
                    matchedTrackIds
                );
            }

            // Step 5: Save results
            operation.DestinationPlaylistId = Guid.Parse(destinationPlaylist.ServicePlaylistId);
            operation.UnmatchedTrackDetails = JsonSerializer.Serialize(
                matchResults.Where(r => !r.IsMatched).Select(r => new
                {
                    r.SourceTrack.ServiceTrackId,
                    r.SourceTrack.Title,
                    r.SourceTrack.Artist,
                    Reason = r.FailureReason,
                    Confidence = r.Confidence
                })
            );

            operation.Status = CopyOperationStatus.Completed;
            operation.CompletedAt = DateTime.UtcNow;
            await UpdateProgressAsync(operation, 100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy operation {OperationId} failed", copyOperationId);

            operation.Status = CopyOperationStatus.Failed;
            operation.ErrorMessage = ex.Message;
            await dbContext.SaveChangesAsync();

            await _hubContext.Clients.User(operation.UserId.ToString())
                .SendAsync("CopyOperationFailed", new
                {
                    OperationId = copyOperationId,
                    Error = ex.Message
                });

            throw; // Trigger Hangfire retry
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task UpdateProgressAsync(CopyOperation operation, int progressPercent)
    {
        operation.ProgressPercent = progressPercent;

        await _hubContext.Clients.User(operation.UserId.ToString())
            .SendAsync("CopyProgress", new
            {
                OperationId = operation.Id,
                Status = operation.Status.ToString(),
                ProgressPercent = progressPercent,
                ProcessedTracks = operation.ProcessedTracks,
                TotalTracks = operation.TotalTracks,
                MatchedTracks = operation.MatchedTracks,
                UnmatchedTracks = operation.UnmatchedTracks
            });
    }
}
```

**Usage**:
```csharp
// Controller enqueues the job
var operationId = Guid.NewGuid();
var operation = new CopyOperation { /* ... */ };
await _dbContext.CopyOperations.AddAsync(operation);
await _dbContext.SaveChangesAsync();

BackgroundJob.Enqueue<IPlaylistCopyJob>(x => x.ExecuteAsync(operationId));
```

---

### 3. Playlist Export Job

Exports platform playlists to music services.

**Trigger**: User-initiated via `/playlist-operations/export` endpoint
**Duration**: Similar to copy operation
**Progress Tracking**: SignalR real-time updates

```csharp
public interface IPlaylistExportJob
{
    Task ExecuteAsync(Guid exportOperationId);
}

public class PlaylistExportJob : IPlaylistExportJob
{
    // Similar to PlaylistCopyJob but:
    // 1. Source is PlatformPlaylist (may contain cross-service tracks)
    // 2. Filters tracks to only those available on destination service
    // 3. For tracks already on destination service, uses direct track ID
    // 4. For tracks from other services, performs matching

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync(Guid exportOperationId)
    {
        // Implementation similar to PlaylistCopyJob
        // See full implementation in codebase
    }
}
```

---

### 4. Token Refresh Job

Automatically refreshes OAuth tokens before they expire.

**Trigger**: Scheduled (every 5 minutes)
**Frequency**: Continuous
**Priority**: High (prevents service disconnections)

```csharp
public interface ITokenRefreshJob
{
    Task RefreshExpiringTokensAsync();
}

public class TokenRefreshJob : ITokenRefreshJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenRefreshJob> _logger;

    public async Task RefreshExpiringTokensAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Find connections with tokens expiring in next 5 minutes
        var expiringConnections = await dbContext.ServiceConnections
            .Where(sc => sc.ConnectionStatus == ConnectionStatus.Active &&
                        sc.AccessTokenExpiresAt <= DateTime.UtcNow.AddMinutes(5))
            .ToListAsync();

        foreach (var connection in expiringConnections)
        {
            try
            {
                var adapter = GetAdapterForService(connection.ServiceType);

                // Only services with refresh tokens (Spotify, Apple Music, Deezer has permanent tokens)
                if (string.IsNullOrEmpty(connection.EncryptedRefreshToken))
                {
                    _logger.LogWarning(
                        "Connection {ConnectionId} has no refresh token",
                        connection.Id
                    );
                    continue;
                }

                var result = await adapter.RefreshTokenAsync(connection.EncryptedRefreshToken);

                connection.EncryptedAccessToken = result.AccessToken;
                connection.AccessTokenExpiresAt = result.ExpiresAt;

                if (!string.IsNullOrEmpty(result.RefreshToken))
                {
                    connection.EncryptedRefreshToken = result.RefreshToken;
                }

                connection.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Refreshed token for connection {ConnectionId} ({ServiceType})",
                    connection.Id,
                    connection.ServiceType
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to refresh token for connection {ConnectionId}",
                    connection.Id
                );

                connection.ConnectionStatus = ConnectionStatus.Expired;
                connection.UpdatedAt = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync();
    }
}
```

**Hangfire Configuration**:
```csharp
RecurringJob.AddOrUpdate<ITokenRefreshJob>(
    "token-refresh",
    x => x.RefreshExpiringTokensAsync(),
    "*/5 * * * *" // Every 5 minutes
);
```

---

### 5. Periodic Playlist Sync Job

Syncs playlist metadata and track lists from connected services.

**Trigger**: Scheduled (every 6 hours for active users)
**Frequency**: Configurable per user
**Priority**: Low (background sync)

```csharp
public interface IPeriodicSyncJob
{
    Task SyncActiveUsersAsync();
    Task SyncUserPlaylistsAsync(Guid userId);
}

public class PeriodicSyncJob : IPeriodicSyncJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PeriodicSyncJob> _logger;

    public async Task SyncActiveUsersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Find users who were active in last 7 days
        var activeUsers = await dbContext.UserAccounts
            .Where(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-7))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in activeUsers)
        {
            BackgroundJob.Enqueue(() => SyncUserPlaylistsAsync(userId));
        }
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SyncUserPlaylistsAsync(Guid userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var connections = await dbContext.ServiceConnections
            .Where(sc => sc.UserId == userId && sc.ConnectionStatus == ConnectionStatus.Active)
            .ToListAsync();

        foreach (var connection in connections)
        {
            try
            {
                var adapter = GetAdapterForService(connection.ServiceType);

                // Fetch latest playlists from service
                var servicePlaylists = await adapter.GetUserPlaylistsAsync(
                    connection.EncryptedAccessToken
                );

                // Update existing, add new, mark deleted
                foreach (var servicePlaylist in servicePlaylists)
                {
                    var existing = await dbContext.ServicePlaylists
                        .FirstOrDefaultAsync(sp =>
                            sp.ServiceConnectionId == connection.Id &&
                            sp.ServicePlaylistId == servicePlaylist.ServicePlaylistId
                        );

                    if (existing != null)
                    {
                        // Update existing
                        existing.Name = servicePlaylist.Name;
                        existing.Description = servicePlaylist.Description;
                        existing.TrackCount = servicePlaylist.TrackCount;
                        existing.ImageUrl = servicePlaylist.ImageUrl;
                        existing.LastSyncedAt = DateTime.UtcNow;
                        existing.SyncStatus = SyncStatus.Synced;
                    }
                    else
                    {
                        // Add new
                        await dbContext.ServicePlaylists.AddAsync(new ServicePlaylist
                        {
                            ServiceConnectionId = connection.Id,
                            ServicePlaylistId = servicePlaylist.ServicePlaylistId,
                            Name = servicePlaylist.Name,
                            Description = servicePlaylist.Description,
                            ServiceOwnerId = servicePlaylist.ServiceOwnerId,
                            IsOwnedByUser = servicePlaylist.IsOwnedByUser,
                            TrackCount = servicePlaylist.TrackCount,
                            ImageUrl = servicePlaylist.ImageUrl,
                            ServiceUrl = servicePlaylist.ServiceUrl,
                            LastSyncedAt = DateTime.UtcNow,
                            SyncStatus = SyncStatus.Synced
                        });
                    }
                }

                // Mark playlists not in response as deleted
                var servicePlaylistIds = servicePlaylists.Select(sp => sp.ServicePlaylistId).ToHashSet();
                var deletedPlaylists = await dbContext.ServicePlaylists
                    .Where(sp => sp.ServiceConnectionId == connection.Id &&
                                !servicePlaylistIds.Contains(sp.ServicePlaylistId))
                    .ToListAsync();

                foreach (var deleted in deletedPlaylists)
                {
                    deleted.SyncStatus = SyncStatus.Deleted;
                }

                connection.LastSyncedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to sync playlists for connection {ConnectionId}",
                    connection.Id
                );
            }
        }
    }
}
```

**Hangfire Configuration**:
```csharp
RecurringJob.AddOrUpdate<IPeriodicSyncJob>(
    "periodic-playlist-sync",
    x => x.SyncActiveUsersAsync(),
    "0 */6 * * *" // Every 6 hours
);
```

---

## Hangfire Dashboard

### Configuration
```csharp
// Startup.cs
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    StatsPollingInterval = 5000, // 5 seconds
    DisplayStorageConnectionString = false
});

app.UseHangfireServer(new BackgroundJobServerOptions
{
    WorkerCount = 10, // Adjust based on server capacity
    Queues = new[] { "critical", "default", "background" },
    ServerName = Environment.MachineName
});
```

### Custom Authorization
```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Only allow authenticated users with Admin role
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
```

---

## Job Monitoring and Alerts

### Metrics to Monitor
- **Sync Queue Depth**: Alert if > 1,000 pending items
- **Failed Jobs**: Alert if > 10% failure rate
- **Processing Time**: Alert if copy operations take > 30 minutes
- **Token Refresh Failures**: Alert if > 5% failure rate

### Implementation
```csharp
public class HangfireMetricsService
{
    public async Task<JobMetrics> GetMetricsAsync()
    {
        var api = JobStorage.Current.GetMonitoringApi();

        return new JobMetrics
        {
            EnqueuedCount = api.EnqueuedCount("default"),
            ScheduledCount = api.ScheduledCount(),
            ProcessingCount = api.ProcessingCount(),
            SucceededCount = api.SucceededListCount(),
            FailedCount = api.FailedCount(),
            DeletedCount = api.DeletedListCount(),
            RecurringJobCount = api.RecurringJobs().Count
        };
    }
}
```

---

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task ProcessQueueItem_ShouldAddTrackToService()
{
    // Arrange
    var queueItem = new SyncQueue
    {
        Id = Guid.NewGuid(),
        OperationType = SyncOperationType.AddTrack,
        OperationPayload = JsonSerializer.Serialize(new { track_id = "spotify:track:abc123", position = 5 }),
        Status = SyncQueueStatus.Pending
    };

    var mockAdapter = new Mock<ISpotifyAdapter>();
    mockAdapter.Setup(x => x.AddTracksAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<IEnumerable<string>>(),
        It.IsAny<int?>()
    )).ReturnsAsync(true);

    var processor = new SyncQueueProcessor(/* dependencies */);

    // Act
    await processor.ProcessQueueItemAsync(queueItem.Id);

    // Assert
    mockAdapter.Verify(x => x.AddTracksAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.Is<IEnumerable<string>>(ids => ids.Contains("spotify:track:abc123")),
        5
    ), Times.Once);

    Assert.Equal(SyncQueueStatus.Completed, queueItem.Status);
}
```

---

## Deployment Checklist

- [ ] Configure Hangfire storage (SQL Server / PostgreSQL)
- [ ] Set up Hangfire dashboard with authentication
- [ ] Configure worker count based on server capacity
- [ ] Set up job queues (critical, default, background)
- [ ] Configure recurring jobs (token refresh, periodic sync, queue processor)
- [ ] Set up monitoring and alerts
- [ ] Test job execution in development
- [ ] Test retry logic and error handling
- [ ] Test SignalR real-time progress updates
- [ ] Configure logging for job execution
- [ ] Document operational procedures (manual job triggering, troubleshooting)

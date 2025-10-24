using Microsoft.Extensions.Logging;
using UniPlay.Core.Services;

namespace UniPlay.Infrastructure.Jobs;

/// <summary>
/// Background job to sync playlists from all connected services
/// Runs periodically to keep playlists up to date
/// </summary>
public class PlaylistSyncJob : BaseJob
{
    private readonly IPlaylistService _playlistService;

    public PlaylistSyncJob(
        IPlaylistService playlistService,
        ILogger<PlaylistSyncJob> logger)
        : base(logger)
    {
        _playlistService = playlistService;
    }

    public override async Task ExecuteAsync()
    {
        Logger.LogInformation("Starting playlist sync job");
        await _playlistService.SyncAllPlaylistsAsync();
        Logger.LogInformation("Playlist sync job completed");
    }
}

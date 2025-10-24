using Microsoft.Extensions.Logging;

namespace UniPlay.Infrastructure.Jobs;

/// <summary>
/// Base class for Hangfire background jobs
/// </summary>
public abstract class BaseJob
{
    protected readonly ILogger Logger;

    protected BaseJob(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Execute the job
    /// </summary>
    public abstract Task ExecuteAsync();

    /// <summary>
    /// Execute the job with error handling
    /// </summary>
    public async Task RunAsync()
    {
        var jobName = GetType().Name;
        Logger.LogInformation("Starting job: {JobName}", jobName);

        try
        {
            await ExecuteAsync();
            Logger.LogInformation("Completed job: {JobName}", jobName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing job: {JobName}", jobName);
            throw; // Re-throw to let Hangfire handle retries
        }
    }
}

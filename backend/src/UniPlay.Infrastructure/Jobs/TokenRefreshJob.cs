using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniPlay.Core.Enums;
using UniPlay.Core.Services;
using UniPlay.Infrastructure.Data;

namespace UniPlay.Infrastructure.Jobs;

/// <summary>
/// Background job to refresh expiring OAuth tokens
/// Runs periodically to ensure tokens don't expire
/// </summary>
public class TokenRefreshJob : BaseJob
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceConnectionService _serviceConnectionService;

    public TokenRefreshJob(
        ApplicationDbContext context,
        IServiceConnectionService serviceConnectionService,
        ILogger<TokenRefreshJob> logger)
        : base(logger)
    {
        _context = context;
        _serviceConnectionService = serviceConnectionService;
    }

    public override async Task ExecuteAsync()
    {
        Logger.LogInformation("Starting token refresh job");

        // Find all connections that will expire in the next hour
        var expiringConnections = await _context.ServiceConnections
            .Where(sc =>
                sc.ConnectionStatus == ConnectionStatus.Active &&
                sc.AccessTokenExpiresAt <= DateTime.UtcNow.AddHours(1) &&
                sc.EncryptedRefreshToken != null)
            .ToListAsync();

        Logger.LogInformation("Found {Count} connections with expiring tokens", expiringConnections.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var connection in expiringConnections)
        {
            try
            {
                await _serviceConnectionService.RefreshConnectionTokenAsync(connection.Id);
                successCount++;
                Logger.LogInformation(
                    "Refreshed token for connection {ConnectionId} ({ServiceType})",
                    connection.Id,
                    connection.ServiceType);
            }
            catch (Exception ex)
            {
                failureCount++;
                Logger.LogError(
                    ex,
                    "Failed to refresh token for connection {ConnectionId} ({ServiceType})",
                    connection.Id,
                    connection.ServiceType);
            }
        }

        Logger.LogInformation(
            "Token refresh job completed: {SuccessCount} succeeded, {FailureCount} failed",
            successCount,
            failureCount);
    }
}

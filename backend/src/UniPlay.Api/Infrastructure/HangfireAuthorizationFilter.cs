using Hangfire.Dashboard;

namespace UniPlay.Api.Infrastructure;

/// <summary>
/// Authorization filter for Hangfire dashboard (development only)
/// In production, this should be replaced with proper authentication
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Allow all requests in development
        // TODO: In production, implement proper authentication
        // Example: Check if user is authenticated and has admin role
        return true;
    }
}

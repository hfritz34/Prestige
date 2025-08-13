using Hangfire.Dashboard;

namespace Prestige.Api.Services
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // In development, allow all access
            // In production, you would implement proper authorization
            return true;
        }
    }
}
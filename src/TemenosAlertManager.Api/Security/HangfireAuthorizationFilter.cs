using Hangfire.Dashboard;

namespace TemenosAlertManager.Api.Security;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Only allow authenticated admin users to access Hangfire dashboard
        var httpContext = context.GetHttpContext();
        
        if (!httpContext.User.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        // Check if user has Admin role
        return httpContext.User.IsInRole("Admin");
    }
}
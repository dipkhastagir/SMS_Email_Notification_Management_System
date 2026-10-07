using Hangfire.Dashboard;

namespace SMSNotificationSystem.Helpers
{
    /// <summary>Only signed-in Administrators can open /hangfire.</summary>
    public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true
                   && httpContext.User.IsInRole(AppRoles.Administrator);
        }
    }
}

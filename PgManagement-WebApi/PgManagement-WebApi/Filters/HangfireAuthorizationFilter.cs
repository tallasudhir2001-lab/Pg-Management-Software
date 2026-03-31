using Hangfire.Dashboard;

namespace PgManagement_WebApi.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // In development, allow access
            if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                return true;

            // In production, require authenticated admin
            return httpContext.User.Identity?.IsAuthenticated == true
                && httpContext.User.IsInRole("Admin");
        }
    }
}

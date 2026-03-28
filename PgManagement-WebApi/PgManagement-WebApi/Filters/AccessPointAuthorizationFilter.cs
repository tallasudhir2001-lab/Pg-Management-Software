using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using PgManagement_WebApi.Attributes;
using System.Text.Json;

namespace PgManagement_WebApi.Filters
{
    public class AccessPointAuthorizationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Only applies to actions decorated with [AccessPoint]
            var apAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<AccessPointAttribute>()
                .FirstOrDefault();

            if (apAttr == null) return;

            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Admins bypass permission checks
            if (user.IsInRole("Admin")) return;

            // Build the key the same way the discovery service does: Module.MethodName
            var methodName = context.ActionDescriptor is ControllerActionDescriptor cad
                ? cad.MethodInfo.Name
                : string.Empty;

            var requiredKey = $"{apAttr.Module}.{methodName}";

            var permissionsClaim = user.FindFirst("permissions")?.Value;
            if (string.IsNullOrEmpty(permissionsClaim))
            {
                context.Result = new ForbidResult();
                return;
            }

            var permissions = JsonSerializer.Deserialize<string[]>(permissionsClaim) ?? [];

            if (!permissions.Contains(requiredKey))
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

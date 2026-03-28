using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.Models;
using System.Reflection;

namespace PgManagement_WebApi.Services
{
    public class AccessPointDiscoveryService : IAccessPointDiscoveryService
    {
        private readonly ApplicationDbContext _context;

        public AccessPointDiscoveryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SyncAccessPointsAsync()
        {
            var discovered = DiscoverAccessPoints();

            var existingInDb = await _context.AccessPoints.ToListAsync();
            var existingByKey = existingInDb.ToDictionary(a => a.Key);
            var discoveredKeys = discovered.Select(d => d.Key).ToHashSet();

            foreach (var item in discovered)
            {
                if (existingByKey.TryGetValue(item.Key, out var existing))
                {
                    // Update mutable fields if changed
                    existing.DisplayName = item.DisplayName;
                    existing.Module = item.Module;
                    existing.Route = item.Route;
                    existing.HttpMethod = item.HttpMethod;
                    existing.IsActive = true;
                }
                else
                {
                    _context.AccessPoints.Add(new AccessPoint
                    {
                        Key = item.Key,
                        Module = item.Module,
                        DisplayName = item.DisplayName,
                        HttpMethod = item.HttpMethod,
                        Route = item.Route,
                        IsActive = true
                    });
                }
            }

            // Mark access points no longer in code as inactive
            foreach (var existing in existingInDb)
            {
                if (!discoveredKeys.Contains(existing.Key))
                    existing.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }

        private record DiscoveredAccessPoint(string Key, string Module, string DisplayName, string HttpMethod, string Route);

        private static List<DiscoveredAccessPoint> DiscoverAccessPoints()
        {
            var results = new List<DiscoveredAccessPoint>();

            var assembly = Assembly.GetExecutingAssembly();
            var controllerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

            foreach (var controller in controllerTypes)
            {
                var controllerRouteAttr = controller.GetCustomAttribute<RouteAttribute>();
                var controllerRouteTemplate = controllerRouteAttr?.Template ?? string.Empty;

                var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (var method in methods)
                {
                    var apAttr = method.GetCustomAttribute<AccessPointAttribute>();
                    if (apAttr == null) continue;

                    var key = $"{apAttr.Module}.{method.Name}";

                    var (httpMethod, actionTemplate) = ResolveHttpMethod(method);

                    var route = string.IsNullOrEmpty(actionTemplate)
                        ? controllerRouteTemplate
                        : $"{controllerRouteTemplate}/{actionTemplate}";

                    results.Add(new DiscoveredAccessPoint(key, apAttr.Module, apAttr.DisplayName, httpMethod, route));
                }
            }

            return results;
        }

        private static (string HttpMethod, string Template) ResolveHttpMethod(MethodInfo method)
        {
            var httpMethodAttrs = method.GetCustomAttributes()
                .OfType<IActionHttpMethodProvider>();

            foreach (var attr in httpMethodAttrs)
            {
                var httpMethod = attr.HttpMethods.FirstOrDefault() ?? "GET";
                var template = (attr as IRouteTemplateProvider)?.Template ?? string.Empty;
                return (httpMethod.ToUpperInvariant(), template);
            }

            return ("GET", string.Empty);
        }
    }
}

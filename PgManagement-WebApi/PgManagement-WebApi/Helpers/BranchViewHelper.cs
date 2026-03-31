using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using System.Text.Json;

namespace PgManagement_WebApi.Helpers
{
    public static class BranchViewHelper
    {
        /// <summary>
        /// Returns the list of PG IDs to filter by.
        /// When X-Branch-View header is "true" and user has Branch.ToggleBranchView permission,
        /// returns all PG IDs the user is assigned to within their branch.
        /// Otherwise returns only the current PG ID from JWT.
        /// </summary>
        public static async Task<List<string>> GetEffectivePgIds(
            this ControllerBase controller,
            ApplicationDbContext context)
        {
            var pgId = controller.User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return new List<string>();

            var branchView = controller.Request.Headers["X-Branch-View"].FirstOrDefault();
            if (!string.Equals(branchView, "true", StringComparison.OrdinalIgnoreCase))
                return new List<string> { pgId };

            // Check permission
            var permsClaim = controller.User.FindFirst("permissions")?.Value;
            if (string.IsNullOrEmpty(permsClaim))
                return new List<string> { pgId };

            var perms = JsonSerializer.Deserialize<string[]>(permsClaim) ?? Array.Empty<string>();
            if (!perms.Contains("Branch.ToggleBranchView"))
                return new List<string> { pgId };

            var branchId = controller.User.FindFirst("branchId")?.Value;
            if (string.IsNullOrEmpty(branchId))
                return new List<string> { pgId };

            var userId = controller.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return new List<string> { pgId };

            // Return only PGs this user is assigned to within the branch
            var userPgIds = await context.UserPgs
                .Where(up => up.UserId == userId)
                .Select(up => up.PgId)
                .ToListAsync();

            var branchPgIds = await context.PGs
                .Where(p => p.BranchId == branchId && userPgIds.Contains(p.PgId))
                .Select(p => p.PgId)
                .ToListAsync();

            return branchPgIds.Any() ? branchPgIds : new List<string> { pgId };
        }
    }
}

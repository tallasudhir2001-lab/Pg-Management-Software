using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.AccessPoint;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class AccessPointAdminService : IAccessPointAdminService
    {
        private readonly ApplicationDbContext _context;

        public AccessPointAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AccessPointModuleDto>> GetAllAsync()
        {
            var accessPoints = await _context.AccessPoints
                .Where(a => a.IsActive)
                .OrderBy(a => a.Module)
                .ThenBy(a => a.DisplayName)
                .ToListAsync();

            return accessPoints
                .GroupBy(a => a.Module)
                .Select(g => new AccessPointModuleDto
                {
                    Module = g.Key,
                    AccessPoints = g.Select(a => new AccessPointDto
                    {
                        Id = a.Id,
                        Key = a.Key,
                        DisplayName = a.DisplayName,
                        HttpMethod = a.HttpMethod,
                        Route = a.Route
                    }).ToList()
                })
                .ToList();
        }

        public async Task<List<int>> GetRoleAccessPointsAsync(string roleId)
        {
            return await _context.RoleAccessPoints
                .Where(rap => rap.RoleId == roleId)
                .Select(rap => rap.AccessPointId)
                .ToListAsync();
        }

        public async Task<(bool success, string? error, int statusCode)> UpdateRoleAccessPointsAsync(
            string roleId, UpdateRoleAccessPointsDto dto)
        {
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                return (false, "Role not found.", 404);

            using var tx = await _context.Database.BeginTransactionAsync();

            var existing = await _context.RoleAccessPoints
                .Where(rap => rap.RoleId == roleId)
                .ToListAsync();

            _context.RoleAccessPoints.RemoveRange(existing);

            var newEntries = dto.AccessPointIds
                .Distinct()
                .Select(apId => new RoleAccessPoint
                {
                    RoleId = roleId,
                    AccessPointId = apId
                });

            await _context.RoleAccessPoints.AddRangeAsync(newEntries);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, null, 204);
        }

        public async Task<object> GetRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.Name != "Admin")
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();
        }
    }
}

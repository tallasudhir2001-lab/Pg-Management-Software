using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.AccessPoint;
using PgManagement_WebApi.Models;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/admin/access-points")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AccessPointAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAccessPointDiscoveryService _discoveryService;

        public AccessPointAdminController(ApplicationDbContext context, IAccessPointDiscoveryService discoveryService)
        {
            _context = context;
            _discoveryService = discoveryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var accessPoints = await _context.AccessPoints
                .Where(a => a.IsActive)
                .OrderBy(a => a.Module)
                .ThenBy(a => a.DisplayName)
                .ToListAsync();

            var grouped = accessPoints
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

            return Ok(grouped);
        }

        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetRoleAccessPoints(string roleId)
        {
            var ids = await _context.RoleAccessPoints
                .Where(rap => rap.RoleId == roleId)
                .Select(rap => rap.AccessPointId)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpPut("role/{roleId}")]
        public async Task<IActionResult> UpdateRoleAccessPoints(string roleId, [FromBody] UpdateRoleAccessPointsDto dto)
        {
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                return NotFound("Role not found.");

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

            return NoContent();
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync()
        {
            await _discoveryService.SyncAccessPointsAsync();
            return Ok(new { message = "Access points synced successfully." });
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .Where(r => r.Name != "Admin")
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            return Ok(roles);
        }
    }
}

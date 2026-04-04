using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.DTOs.AccessPoint;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/admin/access-points")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AccessPointAdminController : ControllerBase
    {
        private readonly IAccessPointAdminService _accessPointAdminService;
        private readonly IAccessPointDiscoveryService _discoveryService;

        public AccessPointAdminController(
            IAccessPointAdminService accessPointAdminService,
            IAccessPointDiscoveryService discoveryService)
        {
            _accessPointAdminService = accessPointAdminService;
            _discoveryService = discoveryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _accessPointAdminService.GetAllAsync());
        }

        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetRoleAccessPoints(string roleId)
        {
            return Ok(await _accessPointAdminService.GetRoleAccessPointsAsync(roleId));
        }

        [HttpPut("role/{roleId}")]
        public async Task<IActionResult> UpdateRoleAccessPoints(string roleId, [FromBody] UpdateRoleAccessPointsDto dto)
        {
            var (success, error, statusCode) = await _accessPointAdminService.UpdateRoleAccessPointsAsync(roleId, dto);
            if (!success) return StatusCode(statusCode, error);
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
            return Ok(await _accessPointAdminService.GetRolesAsync());
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.DTOs.PgUser;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/pg-users")]
    [ApiController]
    [Authorize]
    public class PgUserManagementController : ControllerBase
    {
        private readonly IPgUserManagementService _pgUserService;

        public PgUserManagementController(IPgUserManagementService pgUserService)
        {
            _pgUserService = pgUserService;
        }

        private string? PgId => User.FindFirst("pgId")?.Value;
        private string? BranchId => User.FindFirst("branchId")?.Value;
        private bool IsOwnerOrAdmin => User.IsInRole("Owner") || User.IsInRole("Admin");

        [AccessPoint("PgUser", "Manage Users")]
        [HttpGet("pgs")]
        public async Task<IActionResult> GetBranchPgs()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            return Ok(await _pgUserService.GetBranchPgsAsync(PgId, BranchId));
        }

        [AccessPoint("PgUser", "Manage Users")]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            return Ok(await _pgUserService.GetUsersAsync(PgId, BranchId, IsOwnerOrAdmin));
        }

        [AccessPoint("PgUser", "Add User")]
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddPgUserDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var (success, result, statusCode) = await _pgUserService.AddUserAsync(PgId, BranchId, dto);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("PgUser", "Update User Role")]
        [HttpPut("{userId}/role")]
        public async Task<IActionResult> UpdateRole(string userId, [FromBody] UpdateUserRoleDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var (success, result, statusCode) = await _pgUserService.UpdateRoleAsync(userId, PgId, BranchId, dto);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("PgUser", "Update User PGs")]
        [HttpPut("{userId}/pgs")]
        public async Task<IActionResult> UpdatePgAssignments(string userId, [FromBody] UpdateUserPgsDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var (success, result, statusCode) = await _pgUserService.UpdatePgAssignmentsAsync(userId, PgId, BranchId, dto);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("PgUser", "Remove User")]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveUser(string userId)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var (success, result, statusCode) = await _pgUserService.RemoveUserAsync(userId, PgId, BranchId);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }
    }
}

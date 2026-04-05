using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.DTOs.Admin;
using PgManagement_WebApi.DTOs.Auth;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches()
        {
            return Ok(await _adminService.GetBranchesAsync());
        }

        [HttpGet("pgs")]
        public async Task<IActionResult> GetPgs()
        {
            return Ok(await _adminService.GetPgsAsync());
        }

        [HttpPost("register-pg")]
        public async Task<IActionResult> RegisterPg(PgRegisterRequestDto request)
        {
            var (success, result, statusCode) = await _adminService.RegisterPgAsync(request);
            if (!success) return StatusCode(statusCode, new { message = result });
            return Ok(new { message = result });
        }

        [HttpPut("pgs/{pgId}/subscription")]
        public async Task<IActionResult> UpdatePgSubscription(string pgId, UpdatePgSubscriptionDto dto)
        {
            var (success, result, statusCode) = await _adminService.UpdatePgSubscriptionAsync(pgId, dto);
            if (!success) return StatusCode(statusCode, new { message = result });
            return Ok(new { message = result });
        }

        [HttpPut("pgs/{pgId}/details")]
        public async Task<IActionResult> UpdatePgDetails(string pgId, UpdatePgDetailsDto dto)
        {
            var (success, result, statusCode) = await _adminService.UpdatePgDetailsAsync(pgId, dto);
            if (!success) return StatusCode(statusCode, new { message = result });
            return Ok(new { message = result });
        }
    }
}

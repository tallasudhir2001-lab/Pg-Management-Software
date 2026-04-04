using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Tenant;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;

        public TenantController(ApplicationDbContext context, ITenantService tenantService)
        {
            _context = context;
            _tenantService = tenantService;
        }

        [AccessPoint("Tenant", "View All Tenants")]
        [HttpGet]
        public async Task<IActionResult> GetTenants(
            int page = 1, int pageSize = 10,
            string? search = null, string? status = null, string? roomId = null,
            bool? rentPending = null, bool? advancePending = null, bool? overdueCheckout = null,
            string sortBy = "updated", string sortDir = "desc")
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _tenantService.GetTenantsAsync(pgIds, page, pageSize, search, status,
                roomId, rentPending, advancePending, overdueCheckout, sortBy, sortDir));
        }

        [AccessPoint("Tenant", "View Tenant Details")]
        [HttpGet("{tenantId}")]
        public async Task<IActionResult> GetTenantById(string tenantId)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId, pgIds);
            if (tenant == null) return NotFound();
            return Ok(tenant);
        }

        [AccessPoint("Tenant", "Change Tenant Room")]
        [HttpPost("{tenantId}/change-room")]
        public async Task<IActionResult> ChangeRoom(string tenantId, ChangeRoomDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) =
                await _tenantService.ChangeRoomAsync(tenantId, dto.newRoomId, pgId, dto.changeDate, dto.StayType);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("Tenant", "Change Stay Type")]
        [HttpPost("{tenantId}/change-stay-type")]
        public async Task<IActionResult> ChangeStayType(string tenantId, [FromBody] ChangeStayTypeDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) =
                await _tenantService.ChangeStayTypeAsync(tenantId, dto.NewStayType, pgId, dto.EffectiveDate);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("Tenant", "Set Expected Checkout")]
        [HttpPut("{tenantId}/expected-checkout")]
        public async Task<IActionResult> SetExpectedCheckOut(string tenantId, [FromBody] SetExpectedCheckOutDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _tenantService.SetExpectedCheckOutAsync(tenantId, pgId, dto);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("Tenant", "Move Out Tenant")]
        [HttpPost("{tenantId}/move-out")]
        public async Task<IActionResult> MoveOutTenant(string tenantId, [FromBody] MoveOutDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _tenantService.MoveOutTenantAsync(tenantId, pgId, dto);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("Tenant", "Create Tenant")]
        [HttpPost("create-tenant")]
        public async Task<IActionResult> CreateTenant(CreateTenantDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _tenantService.CreateTenantAsync(dto, pgId, userId, branchId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("Tenant", "Update Tenant")]
        [HttpPut("{tenantId}")]
        public async Task<IActionResult> UpdateTenant(string tenantId, UpdateTenantDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _tenantService.UpdateTenantAsync(tenantId, pgId, dto);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("Tenant", "Delete Tenant")]
        [HttpDelete("{tenantId}")]
        public async Task<IActionResult> DeleteTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _tenantService.DeleteTenantAsync(tenantId, pgId);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [HttpGet("findby-aadhar/{aadhar}")]
        public async Task<IActionResult> GetByAadhar(string aadhar)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var tenant = await _tenantService.FindByAadharNumberAsync(pgId, aadhar);
            if (tenant == null) return NotFound();
            return Ok(tenant);
        }

        [AccessPoint("Tenant", "Check In Tenant")]
        [HttpPost("{tenantId}/create-stay")]
        public async Task<IActionResult> CreateStay(string tenantId, CreateStayDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _tenantService.CreateStayAsync(tenantId, dto, pgId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }
    }
}

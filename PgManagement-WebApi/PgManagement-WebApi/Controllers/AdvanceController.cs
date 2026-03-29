using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.DTOs.advance;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/advances")]
    [ApiController]
    public class AdvanceController : ControllerBase
    {
        private readonly IAdvanceService advanceService;

        public AdvanceController(IAdvanceService advanceService)
        {
            this.advanceService = advanceService;
        }

        [HttpGet("tenant/{tenantId}")]
        public async Task<IActionResult> GetAdvancesByTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var (success, result, statusCode) =
                await advanceService.GetAdvancesByTenantAsync(tenantId, pgId);

            if (!success)
                return StatusCode(statusCode, result);

            return Ok(result);
        }

        [AccessPoint("Advance", "Settle Advance")]
        [HttpPost("{advanceId}/settle")]
        public async Task<IActionResult> SettleAdvance(string advanceId, SettleAdvanceDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var (success, result, statusCode) =
                await advanceService.SettleAdvanceAsync(advanceId, dto, pgId,userId);

            if (!success)
                return StatusCode(statusCode, result);

            return Ok(result);
        }
        [AccessPoint("Advance", "Create Advance")]
        [HttpPost]
        public async Task<IActionResult> CreateAdvance(CreateAdvanceDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var (success, result, statusCode) =
                await advanceService.CreateAdvanceAsync(dto, pgId, userId, branchId);

            if (!success)
                return StatusCode(statusCode, result);

            return Ok(result);
        }

    }
}

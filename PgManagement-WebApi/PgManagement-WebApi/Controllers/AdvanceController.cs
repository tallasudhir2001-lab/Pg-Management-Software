using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        [HttpPost]
        public async Task<IActionResult> CreateAdvance(CreateAdvanceDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var (success, result, statusCode) =
                await advanceService.CreateAdvanceAsync(dto, pgId, userId);

            if (!success)
                return StatusCode(statusCode, result);

            return Ok(result);
        }

    }
}

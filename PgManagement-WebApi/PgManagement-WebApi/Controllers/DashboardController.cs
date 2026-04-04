using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ApplicationDbContext _context;

        public DashboardController(IDashboardService dashboardService, ApplicationDbContext context)
        {
            _dashboardService = dashboardService;
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(DateTime? from, DateTime? to)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _dashboardService.GetSummaryAsync(pgIds, from, to));
        }

        [HttpGet("revenue-trend")]
        public async Task<IActionResult> GetRevenueTrend(DateTime? from, DateTime? to)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _dashboardService.GetRevenueTrendAsync(pgIds, from, to));
        }

        [HttpGet("recent-payments")]
        public async Task<IActionResult> GetRecentPayments(int limit = 5, DateTime? from = null, DateTime? to = null)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _dashboardService.GetRecentPaymentsAsync(pgIds, limit, from, to));
        }

        [HttpGet("occupancy")]
        public async Task<IActionResult> GetOccupancy()
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _dashboardService.GetOccupancyAsync(pgIds));
        }

        [HttpGet("expenses-summary")]
        public async Task<IActionResult> GetExpensesSummary(DateTime? from, DateTime? to)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _dashboardService.GetExpensesSummaryAsync(pgIds, from, to));
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _dashboardService.GetAlertsAsync(pgIds));
        }

        [HttpGet("collection-summary")]
        public async Task<IActionResult> GetCollectionSummary(DateTime? from, DateTime? to)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _dashboardService.GetCollectionSummaryAsync(pgIds, from, to));
        }
    }
}

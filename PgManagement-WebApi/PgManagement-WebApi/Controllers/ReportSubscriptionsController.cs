using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.DTOs.Settings;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/settings/report-subscriptions")]
    [ApiController]
    [Authorize]
    public class ReportSubscriptionsController : ControllerBase
    {
        private readonly IReportSubscriptionService _reportSubscriptionService;

        public ReportSubscriptionsController(IReportSubscriptionService reportSubscriptionService)
        {
            _reportSubscriptionService = reportSubscriptionService;
        }

        [HttpGet("report-options")]
        public IActionResult GetReportOptions()
        {
            return Ok(_reportSubscriptionService.GetReportOptions());
        }

        [AccessPoint("Settings", "View Report Subscriptions")]
        [HttpGet]
        public async Task<IActionResult> GetReportSubscriptions()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            return Ok(await _reportSubscriptionService.GetReportSubscriptionsAsync(pgId));
        }

        [AccessPoint("Settings", "Update Report Subscriptions")]
        [HttpPut]
        public async Task<IActionResult> UpdateReportSubscriptions([FromBody] UpdateReportSubscriptionsRequest request)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _reportSubscriptionService.UpdateReportSubscriptionsAsync(pgId, request);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }
    }
}

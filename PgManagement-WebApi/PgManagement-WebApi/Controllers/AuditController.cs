using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/audit")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public AuditController(IAuditService auditService, ApplicationDbContext context)
        {
            _auditService = auditService;
            _context = context;
        }

        [HttpGet("unreviewed-count")]
        [AccessPoint("Audit", "View unreviewed audit count")]
        public async Task<IActionResult> GetUnreviewedCount()
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _auditService.GetUnreviewedCountAsync(pgIds));
        }

        [HttpGet]
        [AccessPoint("Audit", "View audit events")]
        public async Task<IActionResult> GetAuditEvents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? eventType = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? status = null,
            [FromQuery] string sortDir = "desc")
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _auditService.GetAuditEventsAsync(pgIds, page, pageSize, eventType, entityType, status, sortDir));
        }

        [HttpPost("{id}/review")]
        [AccessPoint("Audit", "Mark audit event as reviewed")]
        public async Task<IActionResult> MarkAsReviewed(string id)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var (success, result, statusCode) = await _auditService.MarkAsReviewedAsync(id, pgIds, userId);
            return StatusCode(statusCode, result);
        }

        [HttpPost("review-all")]
        [AccessPoint("Audit", "Mark all audit events as reviewed")]
        public async Task<IActionResult> MarkAllAsReviewed()
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var (success, result, statusCode) = await _auditService.MarkAllAsReviewedAsync(pgIds, userId);
            return StatusCode(statusCode, result);
        }
    }
}

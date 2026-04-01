using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Audit;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.Helpers;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/audit")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("unreviewed-count")]
        [AccessPoint("Audit", "View unreviewed audit count")]
        public async Task<IActionResult> GetUnreviewedCount()
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any())
                return Unauthorized();

            var count = await _context.AuditEvents
                .AsNoTracking()
                .CountAsync(a => pgIds.Contains(a.PgId) && !a.IsReviewed);

            return Ok(new AuditCountDto { UnreviewedCount = count });
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
            if (!pgIds.Any())
                return Unauthorized();

            var query = _context.AuditEvents
                .AsNoTracking()
                .Include(a => a.PerformedByUser)
                .Include(a => a.ReviewedByUser)
                .Where(a => pgIds.Contains(a.PgId));

            if (!string.IsNullOrEmpty(eventType))
                query = query.Where(a => a.EventType == eventType);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("reviewed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(a => a.IsReviewed);
                else if (status.Equals("unreviewed", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(a => !a.IsReviewed);
            }

            var totalCount = await query.CountAsync();

            query = sortDir == "asc"
                ? query.OrderBy(a => a.PerformedAt)
                : query.OrderByDescending(a => a.PerformedAt);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditEventListDto
                {
                    Id = a.Id,
                    EventType = a.EventType,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    Description = a.Description,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    PerformedBy = a.PerformedByUser.FullName ?? a.PerformedByUser.UserName!,
                    PerformedAt = a.PerformedAt,
                    IsReviewed = a.IsReviewed,
                    ReviewedBy = a.ReviewedByUser != null
                        ? a.ReviewedByUser.FullName ?? a.ReviewedByUser.UserName
                        : null,
                    ReviewedAt = a.ReviewedAt
                })
                .ToListAsync();

            return Ok(new PageResultsDto<AuditEventListDto>
            {
                Items = items,
                TotalCount = totalCount
            });
        }

        [HttpPost("{id}/review")]
        [AccessPoint("Audit", "Mark audit event as reviewed")]
        public async Task<IActionResult> MarkAsReviewed(string id)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any())
                return Unauthorized();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var audit = await _context.AuditEvents
                .FirstOrDefaultAsync(a => a.Id == id && pgIds.Contains(a.PgId));

            if (audit == null)
                return NotFound();

            if (audit.IsReviewed)
                return Ok(new { message = "Already reviewed" });

            audit.IsReviewed = true;
            audit.ReviewedByUserId = userId;
            audit.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Marked as reviewed" });
        }

        [HttpPost("review-all")]
        [AccessPoint("Audit", "Mark all audit events as reviewed")]
        public async Task<IActionResult> MarkAllAsReviewed()
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any())
                return Unauthorized();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var unreviewed = await _context.AuditEvents
                .Where(a => pgIds.Contains(a.PgId) && !a.IsReviewed)
                .ToListAsync();

            foreach (var audit in unreviewed)
            {
                audit.IsReviewed = true;
                audit.ReviewedByUserId = userId;
                audit.ReviewedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"{unreviewed.Count} events marked as reviewed" });
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Audit;
using PgManagement_WebApi.DTOs.Pagination;

namespace PgManagement_WebApi.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuditCountDto> GetUnreviewedCountAsync(List<string> pgIds)
        {
            var count = await _context.AuditEvents
                .AsNoTracking()
                .CountAsync(a => pgIds.Contains(a.PgId) && !a.IsReviewed);

            return new AuditCountDto { UnreviewedCount = count };
        }

        public async Task<PageResultsDto<AuditEventListDto>> GetAuditEventsAsync(
            List<string> pgIds, int page, int pageSize,
            string? eventType, string? entityType, string? status, string sortDir)
        {
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

            return new PageResultsDto<AuditEventListDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<(bool success, object result, int statusCode)> MarkAsReviewedAsync(
            string id, List<string> pgIds, string userId)
        {
            var audit = await _context.AuditEvents
                .FirstOrDefaultAsync(a => a.Id == id && pgIds.Contains(a.PgId));

            if (audit == null)
                return (false, "Not found", 404);

            if (audit.IsReviewed)
                return (true, new { message = "Already reviewed" }, 200);

            audit.IsReviewed = true;
            audit.ReviewedByUserId = userId;
            audit.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, new { message = "Marked as reviewed" }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> MarkAllAsReviewedAsync(
            List<string> pgIds, string userId)
        {
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
            return (true, new { message = $"{unreviewed.Count} events marked as reviewed" }, 200);
        }
    }
}

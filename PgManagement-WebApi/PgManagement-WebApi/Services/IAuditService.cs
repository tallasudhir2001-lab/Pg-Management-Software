using PgManagement_WebApi.DTOs.Audit;
using PgManagement_WebApi.DTOs.Pagination;

namespace PgManagement_WebApi.Services
{
    public interface IAuditService
    {
        Task<AuditCountDto> GetUnreviewedCountAsync(List<string> pgIds);
        Task<PageResultsDto<AuditEventListDto>> GetAuditEventsAsync(
            List<string> pgIds, int page, int pageSize,
            string? eventType, string? entityType, string? status, string sortDir);
        Task<(bool success, object result, int statusCode)> MarkAsReviewedAsync(string id, List<string> pgIds, string userId);
        Task<(bool success, object result, int statusCode)> MarkAllAsReviewedAsync(List<string> pgIds, string userId);
    }
}

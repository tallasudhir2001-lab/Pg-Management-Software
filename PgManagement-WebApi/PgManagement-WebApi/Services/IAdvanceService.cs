using PgManagement_WebApi.DTOs.advance;

namespace PgManagement_WebApi.Services
{
    public interface IAdvanceService
    {
        Task<(bool success, object result, int statusCode)> SettleAdvanceAsync(
    string advanceId,
    SettleAdvanceDto dto,
    string pgId,string userId);
        Task<(bool success, object result, int statusCode)> CreateAdvanceAsync(
    CreateAdvanceDto dto,
    string pgId,
    string userId,
    string? branchId = null);
        Task<(bool success, object result, int statusCode)> GetAdvancesByTenantAsync(
    string tenantId,
    string pgId);
    }
}

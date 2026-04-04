using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Tenant;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public interface ITenantService
    {
        Task<(bool success, object? result, int statusCode)> CreateTenantAsync(
        CreateTenantDto dto,
        string pgId, string userId, string? branchId = null);

        Task<bool> TenantHasActiveStay(string tenantId, string pgId);

        Task<Tenant?> FindByAadhaar(string pgId, string aadhaar);
        Task<(bool success, object? result, int statusCode)> CreateStayAsync(
        string tenantId,
        CreateStayDto dto,
        string pgId
    );
        Task<(bool success, string? error, int statusCode)> CreateStayInternal(string tenantId,
    string roomId,
    DateTime fromDate,
    string pgId,
    string stayType = "MONTHLY");
        Task<(bool success, object? result, int statusCode)> ChangeRoomAsync(
    string tenantId,
    string newRoomId,
    string pgId, DateTime changeDate,
    string stayType = "MONTHLY");

        Task<(bool success, object? result, int statusCode)> ChangeStayTypeAsync(
    string tenantId,
    string newStayType,
    string pgId,
    DateTime effectiveDate);

        Task<PageResultsDto<TenantListDto>> GetTenantsAsync(
            List<string> pgIds, int page, int pageSize, string? search, string? status,
            string? roomId, bool? rentPending, bool? advancePending, bool? overdueCheckout,
            string sortBy, string sortDir);
        Task<object?> GetTenantByIdAsync(string tenantId, List<string> pgIds);
        Task<(bool success, object result, int statusCode)> SetExpectedCheckOutAsync(
            string tenantId, string pgId, SetExpectedCheckOutDto dto);
        Task<(bool success, object result, int statusCode)> MoveOutTenantAsync(
            string tenantId, string pgId, MoveOutDto dto);
        Task<(bool success, object result, int statusCode)> UpdateTenantAsync(
            string tenantId, string pgId, UpdateTenantDto dto);
        Task<(bool success, object result, int statusCode)> DeleteTenantAsync(
            string tenantId, string pgId);
        Task<object?> FindByAadharNumberAsync(string pgId, string aadhar);
    }
}

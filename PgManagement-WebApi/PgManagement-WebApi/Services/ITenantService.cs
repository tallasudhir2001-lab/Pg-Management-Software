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
    }
}

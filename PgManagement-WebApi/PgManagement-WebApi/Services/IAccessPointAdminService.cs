using PgManagement_WebApi.DTOs.AccessPoint;

namespace PgManagement_WebApi.Services
{
    public interface IAccessPointAdminService
    {
        Task<List<AccessPointModuleDto>> GetAllAsync();
        Task<List<int>> GetRoleAccessPointsAsync(string roleId);
        Task<(bool success, string? error, int statusCode)> UpdateRoleAccessPointsAsync(string roleId, UpdateRoleAccessPointsDto dto);
        Task<object> GetRolesAsync();
    }
}

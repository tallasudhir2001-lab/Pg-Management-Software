using PgManagement_WebApi.DTOs.PgUser;

namespace PgManagement_WebApi.Services
{
    public interface IPgUserManagementService
    {
        Task<List<BranchPgInfoDto>> GetBranchPgsAsync(string pgId, string? branchId);
        Task<List<PgUserDto>> GetUsersAsync(string pgId, string? branchId, bool isOwnerOrAdmin);
        Task<(bool success, object result, int statusCode)> AddUserAsync(
            string pgId, string? branchId, AddPgUserDto dto);
        Task<(bool success, object result, int statusCode)> UpdateRoleAsync(
            string userId, string pgId, string? branchId, UpdateUserRoleDto dto);
        Task<(bool success, object result, int statusCode)> UpdatePgAssignmentsAsync(
            string userId, string pgId, string? branchId, UpdateUserPgsDto dto);
        Task<(bool success, object result, int statusCode)> RemoveUserAsync(
            string userId, string pgId, string? branchId);
    }
}

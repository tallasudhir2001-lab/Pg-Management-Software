using PgManagement_WebApi.DTOs.PgUser;

namespace PgManagement_WebApi.Services
{
    public interface IBranchService
    {
        Task<List<BranchPgInfoDto>> GetUserBranchPgsAsync(string userId, string branchId);
    }
}

using PgManagement_WebApi.DTOs.Admin;
using PgManagement_WebApi.DTOs.Auth;

namespace PgManagement_WebApi.Services
{
    public interface IAdminService
    {
        Task<List<BranchDto>> GetBranchesAsync();
        Task<List<PgListDto>> GetPgsAsync();
        Task<(bool success, object result, int statusCode)> RegisterPgAsync(PgRegisterRequestDto request);
        Task<(bool success, object result, int statusCode)> UpdatePgSubscriptionAsync(string pgId, UpdatePgSubscriptionDto dto);
        Task<(bool success, object result, int statusCode)> UpdatePgDetailsAsync(string pgId, UpdatePgDetailsDto dto);
    }
}

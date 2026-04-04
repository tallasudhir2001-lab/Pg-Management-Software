using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Users;

namespace PgManagement_WebApi.Services
{
    public interface IUserService
    {
        Task<PageResultsDto<UserListDto>> GetUsersAsync(string pgId, int page, int pageSize);
    }
}

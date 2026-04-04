using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Users;

namespace PgManagement_WebApi.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResultsDto<UserListDto>> GetUsersAsync(string pgId, int page, int pageSize)
        {
            var query = _context.UserPgs
                .Where(up => up.PgId == pgId)
                .Select(up => up.User)
                .Where(u =>
                    _context.Payments.Any(p =>
                        p.CreatedByUserId == u.Id &&
                        p.PgId == pgId));

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListDto
                {
                    UserId = u.Id,
                    Name = u.UserName!
                })
                .ToListAsync();

            return new PageResultsDto<UserListDto>
            {
                Items = users,
                TotalCount = totalCount
            };
        }
    }
}

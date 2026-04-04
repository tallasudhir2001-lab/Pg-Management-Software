using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.PgUser;

namespace PgManagement_WebApi.Services
{
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _context;

        public BranchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BranchPgInfoDto>> GetUserBranchPgsAsync(string userId, string branchId)
        {
            return await _context.UserPgs
                .Where(up => up.UserId == userId && up.PG.BranchId == branchId)
                .Select(up => new BranchPgInfoDto
                {
                    PgId = up.PgId,
                    Name = up.PG.Name
                })
                .ToListAsync();
        }
    }
}

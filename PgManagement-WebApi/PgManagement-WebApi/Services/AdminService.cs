using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Admin;
using PgManagement_WebApi.DTOs.Auth;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static string SanitiseUserName(string email)
        {
            var local = email.Split('@')[0];
            var allowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._@";
            var clean = new string(local.Where(c => allowed.Contains(c)).ToArray());
            return string.IsNullOrEmpty(clean) ? Guid.NewGuid().ToString("N")[..8] : clean;
        }

        public async Task<List<BranchDto>> GetBranchesAsync()
        {
            var branches = await _context.Branches
                .Include(b => b.PGs)
                .ToListAsync();

            return branches.Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                PgCount = b.PGs.Count,
                PGs = b.PGs.Select(p => new BranchPgDto { PgId = p.PgId, Name = p.Name }).ToList()
            }).ToList();
        }

        public async Task<List<PgListDto>> GetPgsAsync()
        {
            var pgs = await _context.PGs
                .Include(p => p.Branch)
                .ToListAsync();

            var result = new List<PgListDto>();

            foreach (var pg in pgs)
            {
                var userPgs = await _context.UserPgs
                    .Where(up => up.PgId == pg.PgId)
                    .Include(up => up.User)
                    .ToListAsync();

                string ownerName = string.Empty, ownerEmail = string.Empty;
                foreach (var up in userPgs)
                {
                    if (await _userManager.IsInRoleAsync(up.User, "Owner"))
                    {
                        ownerName = up.User.UserName ?? string.Empty;
                        ownerEmail = up.User.Email ?? string.Empty;
                        break;
                    }
                }

                result.Add(new PgListDto
                {
                    PgId = pg.PgId,
                    Name = pg.Name,
                    Address = pg.Address,
                    ContactNumber = pg.ContactNumber,
                    OwnerName = ownerName,
                    OwnerEmail = ownerEmail,
                    UserCount = userPgs.Count,
                    BranchId = pg.BranchId,
                    BranchName = pg.Branch?.Name,
                    IsEmailSubscriptionEnabled = pg.IsEmailSubscriptionEnabled,
                    IsWhatsappSubscriptionEnabled = pg.IsWhatsappSubscriptionEnabled
                });
            }

            return result;
        }

        public async Task<(bool success, object result, int statusCode)> RegisterPgAsync(PgRegisterRequestDto request)
        {
            Branch branch;

            if (!string.IsNullOrEmpty(request.BranchId))
            {
                branch = await _context.Branches
                    .Include(b => b.PGs)
                    .FirstOrDefaultAsync(b => b.Id == request.BranchId);

                if (branch == null)
                    return (false, "Branch not found.", 400);
            }
            else
            {
                branch = new Branch { Name = request.PgName };
                _context.Branches.Add(branch);
            }

            ApplicationUser user = null;

            if (!string.IsNullOrEmpty(request.OwnerEmail))
            {
                user = await _userManager.FindByEmailAsync(request.OwnerEmail);
                if (user == null)
                {
                    if (string.IsNullOrEmpty(request.Password))
                        return (false, "Password is required when creating a new user.", 400);

                    user = new ApplicationUser
                    {
                        UserName = SanitiseUserName(request.OwnerEmail),
                        FullName = request.OwnerName,
                        Email = request.OwnerEmail,
                        EmailConfirmed = true
                    };
                    var createResult = await _userManager.CreateAsync(user, request.Password);
                    if (!createResult.Succeeded)
                        return (false, createResult.Errors, 400);
                }
            }

            var pg = new PG
            {
                PgId = Guid.NewGuid().ToString(),
                Name = request.PgName,
                Address = request.Address,
                ContactNumber = request.ContactNumber,
                Branch = branch
            };
            _context.PGs.Add(pg);

            if (user != null && !await _userManager.IsInRoleAsync(user, "Owner"))
                await _userManager.AddToRoleAsync(user, "Owner");

            if (user != null)
            {
                var alreadyInPg = await _context.UserPgs.AnyAsync(up => up.UserId == user.Id && up.PgId == pg.PgId);
                if (!alreadyInPg)
                    _context.UserPgs.Add(new UserPg { UserId = user.Id, PgId = pg.PgId });
            }

            if (!string.IsNullOrEmpty(request.BranchId) && branch.PGs.Any())
            {
                var existingBranchPgIds = branch.PGs.Select(p => p.PgId).ToList();
                var branchUserIds = await _context.UserPgs
                    .Where(up => existingBranchPgIds.Contains(up.PgId))
                    .Select(up => up.UserId)
                    .Distinct()
                    .ToListAsync();

                foreach (var uid in branchUserIds)
                {
                    var branchUser = await _userManager.FindByIdAsync(uid);
                    if (branchUser != null && await _userManager.IsInRoleAsync(branchUser, "Owner"))
                    {
                        var alreadyIn = await _context.UserPgs.AnyAsync(up => up.UserId == uid && up.PgId == pg.PgId);
                        if (!alreadyIn)
                            _context.UserPgs.Add(new UserPg { UserId = uid, PgId = pg.PgId });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return (true, new PgResisterResponseDto
            {
                UserId = user?.Id ?? string.Empty,
                PgId = pg.PgId,
            }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> UpdatePgSubscriptionAsync(
            string pgId, UpdatePgSubscriptionDto dto)
        {
            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null)
                return (false, "PG not found.", 404);

            pg.IsEmailSubscriptionEnabled = dto.IsEmailSubscriptionEnabled;
            pg.IsWhatsappSubscriptionEnabled = dto.IsWhatsappSubscriptionEnabled;

            await _context.SaveChangesAsync();

            return (true, new PgSubscriptionDto
            {
                PgId = pg.PgId,
                PgName = pg.Name,
                IsEmailSubscriptionEnabled = pg.IsEmailSubscriptionEnabled,
                IsWhatsappSubscriptionEnabled = pg.IsWhatsappSubscriptionEnabled
            }, 200);
        }
    }
}

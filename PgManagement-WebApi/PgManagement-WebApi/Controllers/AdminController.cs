using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Admin;
using PgManagement_WebApi.DTOs.Auth;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
        }

        // Converts email → safe UserName slug, e.g. "john@example.com" → "john"
        private static string SanitiseUserName(string email)
        {
            var local = email.Split('@')[0];
            var allowed = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._@";
            var clean = new string(local.Where(c => allowed.Contains(c)).ToArray());
            return string.IsNullOrEmpty(clean) ? Guid.NewGuid().ToString("N")[..8] : clean;
        }

        // GET /api/admin/branches
        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await context.Branches
                .Include(b => b.PGs)
                .ToListAsync();

            var result = branches.Select(b => new BranchDto
            {
                Id = b.Id,
                Name = b.Name,
                PgCount = b.PGs.Count,
                PGs = b.PGs.Select(p => new BranchPgDto { PgId = p.PgId, Name = p.Name }).ToList()
            }).ToList();

            return Ok(result);
        }

        // GET /api/admin/pgs
        [HttpGet("pgs")]
        public async Task<IActionResult> GetPgs()
        {
            var pgs = await context.PGs
                .Include(p => p.Branch)
                .ToListAsync();

            var result = new List<PgListDto>();

            foreach (var pg in pgs)
            {
                var userPgs = await context.UserPgs
                    .Where(up => up.PgId == pg.PgId)
                    .Include(up => up.User)
                    .ToListAsync();

                string ownerName = string.Empty, ownerEmail = string.Empty;
                foreach (var up in userPgs)
                {
                    if (await userManager.IsInRoleAsync(up.User, "Owner"))
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

            return Ok(result);
        }

        // POST /api/admin/register-pg
        [HttpPost("register-pg")]
        public async Task<IActionResult> RegisterPg(PgRegisterRequestDto request)
        {
            Branch branch;

            if (!string.IsNullOrEmpty(request.BranchId))
            {
                // Adding a second PG to an existing branch
                branch = await context.Branches
                    .Include(b => b.PGs)
                    .FirstOrDefaultAsync(b => b.Id == request.BranchId);

                if (branch == null)
                    return BadRequest("Branch not found.");
            }
            else
            {
                // New branch — auto-create with PG name
                branch = new Branch { Name = request.PgName };
                context.Branches.Add(branch);
            }

            // Find or create owner user
            ApplicationUser user = null;
            bool isNewUser = false;

            if (!string.IsNullOrEmpty(request.OwnerEmail))
            {
                user = await userManager.FindByEmailAsync(request.OwnerEmail);
                if (user == null)
                {
                    if (string.IsNullOrEmpty(request.Password))
                        return BadRequest("Password is required when creating a new user.");

                    user = new ApplicationUser
                    {
                        UserName = SanitiseUserName(request.OwnerEmail),
                        FullName = request.OwnerName,
                        Email = request.OwnerEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, request.Password);
                    if (!result.Succeeded)
                        return BadRequest(result.Errors);

                    isNewUser = true;
                }
            }

            // Create new PG
            var pg = new PG
            {
                PgId = Guid.NewGuid().ToString(),
                Name = request.PgName,
                Address = request.Address,
                ContactNumber = request.ContactNumber,
                Branch = branch
            };
            context.PGs.Add(pg);

            // Assign Owner role to user (only if needed)
            if (user != null && !await userManager.IsInRoleAsync(user, "Owner"))
                await userManager.AddToRoleAsync(user, "Owner");

            // Add UserPg record for the specified owner
            if (user != null)
            {
                var alreadyInPg = await context.UserPgs.AnyAsync(up => up.UserId == user.Id && up.PgId == pg.PgId);
                if (!alreadyInPg)
                    context.UserPgs.Add(new UserPg { UserId = user.Id, PgId = pg.PgId });
            }

            // If adding to existing branch, auto-assign all existing branch owners to the new PG
            if (!string.IsNullOrEmpty(request.BranchId) && branch.PGs.Any())
            {
                var existingBranchPgIds = branch.PGs.Select(p => p.PgId).ToList();
                var branchUserIds = await context.UserPgs
                    .Where(up => existingBranchPgIds.Contains(up.PgId))
                    .Select(up => up.UserId)
                    .Distinct()
                    .ToListAsync();

                foreach (var uid in branchUserIds)
                {
                    var branchUser = await userManager.FindByIdAsync(uid);
                    if (branchUser != null && await userManager.IsInRoleAsync(branchUser, "Owner"))
                    {
                        var alreadyIn = await context.UserPgs.AnyAsync(up => up.UserId == uid && up.PgId == pg.PgId);
                        if (!alreadyIn)
                            context.UserPgs.Add(new UserPg { UserId = uid, PgId = pg.PgId });
                    }
                }
            }

            await context.SaveChangesAsync();

            return Ok(new PgResisterResponseDto
            {
                UserId = user?.Id ?? string.Empty,
                PgId = pg.PgId,
            });
        }

        // PUT /api/admin/pgs/{pgId}/subscription
        [HttpPut("pgs/{pgId}/subscription")]
        public async Task<IActionResult> UpdatePgSubscription(string pgId, UpdatePgSubscriptionDto dto)
        {
            var pg = await context.PGs.FindAsync(pgId);
            if (pg == null)
                return NotFound("PG not found.");

            pg.IsEmailSubscriptionEnabled = dto.IsEmailSubscriptionEnabled;
            pg.IsWhatsappSubscriptionEnabled = dto.IsWhatsappSubscriptionEnabled;

            await context.SaveChangesAsync();

            return Ok(new PgSubscriptionDto
            {
                PgId = pg.PgId,
                PgName = pg.Name,
                IsEmailSubscriptionEnabled = pg.IsEmailSubscriptionEnabled,
                IsWhatsappSubscriptionEnabled = pg.IsWhatsappSubscriptionEnabled
            });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.PgUser;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/pg-users")]
    [ApiController]
    [Authorize]
    public class PgUserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] AllowedRoles = ["Owner", "Manager", "Staff"];

        public PgUserManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        private string? PgId => User.FindFirst("pgId")?.Value;
        private string? BranchId => User.FindFirst("branchId")?.Value;
        private string? UserRole => User.FindFirst("role")?.Value;
        private bool IsOwnerOrAdmin => User.IsInRole("Owner") || User.IsInRole("Admin");

        // ── helpers ──────────────────────────────────────────────────────────

        /// Returns all PgIds in the caller's branch.
        private async Task<List<string>> GetBranchPgIds()
        {
            if (string.IsNullOrEmpty(BranchId)) return string.IsNullOrEmpty(PgId) ? [] : [PgId!];
            return await _context.PGs
                .Where(p => p.BranchId == BranchId)
                .Select(p => p.PgId)
                .ToListAsync();
        }

        /// Builds a PgUserDto for a user, showing which PGs (within scope) they're in.
        private async Task<PgUserDto> BuildUserDto(ApplicationUser user, List<string> scopePgIds)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault(r => r != "Admin") ?? string.Empty;

            var assignedPgs = await _context.UserPgs
                .Where(up => up.UserId == user.Id && scopePgIds.Contains(up.PgId))
                .Include(up => up.PG)
                .Select(up => new AssignedPgDto { PgId = up.PgId, PgName = up.PG.Name })
                .ToListAsync();

            return new PgUserDto
            {
                UserId = user.Id,
                Name = user.FullName ?? user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = roleName,
                AssignedPgs = assignedPgs
            };
        }

        // ── endpoints ────────────────────────────────────────────────────────

        // GET /api/pg-users/pgs — list of PGs in the branch (for the add-user form)
        [AccessPoint("PgUser", "Manage Users")]
        [HttpGet("pgs")]
        public async Task<IActionResult> GetBranchPgs()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();

            var pgIds = await GetBranchPgIds();
            var pgs = await _context.PGs
                .Where(p => pgIds.Contains(p.PgId))
                .Select(p => new BranchPgInfoDto { PgId = p.PgId, Name = p.Name })
                .ToListAsync();

            return Ok(pgs);
        }

        // GET /api/pg-users
        [AccessPoint("PgUser", "Manage Users")]
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();

            // Owner/Admin see all users across the branch; others see only current PG
            var scopePgIds = IsOwnerOrAdmin ? await GetBranchPgIds() : [PgId!];
            // Always show full branch PG assignments so the PG-edit checkboxes reflect reality
            var branchPgIds = IsOwnerOrAdmin ? scopePgIds : await GetBranchPgIds();

            var userIds = await _context.UserPgs
                .Where(up => scopePgIds.Contains(up.PgId))
                .Select(up => up.UserId)
                .Distinct()
                .ToListAsync();

            var result = new List<PgUserDto>();
            foreach (var uid in userIds)
            {
                var user = await _userManager.FindByIdAsync(uid);
                if (user != null)
                    result.Add(await BuildUserDto(user, branchPgIds));
            }

            return Ok(result);
        }

        // POST /api/pg-users
        [AccessPoint("PgUser", "Add User")]
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddPgUserDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();

            if (!AllowedRoles.Contains(dto.RoleName))
                return BadRequest("Invalid role. Must be Owner, Manager, or Staff.");

            var branchPgIds = await GetBranchPgIds();

            // Validate requested PG assignments are within the branch
            var targetPgIds = dto.PgIds.Any()
                ? dto.PgIds.Intersect(branchPgIds).ToList()
                : [PgId!];

            if (!targetPgIds.Any())
                return BadRequest("No valid PG IDs provided within this branch.");

            // Find or create user
            var user = await _userManager.FindByEmailAsync(dto.Email);
            bool isNewUser = user == null;

            if (user == null)
            {
                if (string.IsNullOrEmpty(dto.Password))
                    return BadRequest("Password is required when creating a new user.");

                user = new ApplicationUser
                {
                    UserName = SanitiseUserName(dto.Email),
                    FullName = dto.Name,
                    Email = dto.Email,
                    EmailConfirmed = true
                };
                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                    return BadRequest(createResult.Errors.Select(e => e.Description));
            }

            // Assign role — only set role if user has no non-admin role yet
            var existingRoles = await _userManager.GetRolesAsync(user);
            var nonAdminRoles = existingRoles.Where(r => r != "Admin").ToList();
            if (!nonAdminRoles.Any())
            {
                await _userManager.AddToRoleAsync(user, dto.RoleName);
            }

            // Add UserPg records for target PGs (skip if already assigned)
            foreach (var pgId in targetPgIds)
            {
                var alreadyIn = await _context.UserPgs.AnyAsync(up => up.UserId == user.Id && up.PgId == pgId);
                if (!alreadyIn)
                    _context.UserPgs.Add(new UserPg { UserId = user.Id, PgId = pgId });
            }

            await _context.SaveChangesAsync();

            return Ok(await BuildUserDto(user, branchPgIds));
        }

        // PUT /api/pg-users/{userId}/role
        [AccessPoint("PgUser", "Update User Role")]
        [HttpPut("{userId}/role")]
        public async Task<IActionResult> UpdateRole(string userId, [FromBody] UpdateUserRoleDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();

            if (!AllowedRoles.Contains(dto.RoleName))
                return BadRequest("Invalid role. Must be Owner, Manager, or Staff.");

            var branchPgIds = await GetBranchPgIds();
            var inBranch = await _context.UserPgs.AnyAsync(up => up.UserId == userId && branchPgIds.Contains(up.PgId));
            if (!inBranch) return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var nonAdminRoles = currentRoles.Where(r => r != "Admin").ToList();
            if (nonAdminRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, nonAdminRoles);
            await _userManager.AddToRoleAsync(user, dto.RoleName);

            return NoContent();
        }

        // PUT /api/pg-users/{userId}/pgs — update which branch PGs this user is assigned to
        [AccessPoint("PgUser", "Update User PGs")]
        [HttpPut("{userId}/pgs")]
        public async Task<IActionResult> UpdatePgAssignments(string userId, [FromBody] UpdateUserPgsDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();

            var branchPgIds = await GetBranchPgIds();
            var inBranch = await _context.UserPgs.AnyAsync(up => up.UserId == userId && branchPgIds.Contains(up.PgId));
            if (!inBranch) return NotFound();

            var validTargetPgIds = dto.PgIds.Intersect(branchPgIds).ToList();
            if (!validTargetPgIds.Any())
                return BadRequest("At least one valid PG must be assigned.");

            // Remove existing branch assignments
            var existingAssignments = await _context.UserPgs
                .Where(up => up.UserId == userId && branchPgIds.Contains(up.PgId))
                .ToListAsync();
            _context.UserPgs.RemoveRange(existingAssignments);

            // Add new assignments
            foreach (var pgId in validTargetPgIds)
                _context.UserPgs.Add(new UserPg { UserId = userId, PgId = pgId });

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/pg-users/{userId} — remove user from all PGs in the branch
        [AccessPoint("PgUser", "Remove User")]
        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveUser(string userId)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();

            var branchPgIds = await GetBranchPgIds();
            var userPgs = await _context.UserPgs
                .Where(up => up.UserId == userId && branchPgIds.Contains(up.PgId))
                .ToListAsync();

            if (!userPgs.Any()) return NotFound();

            _context.UserPgs.RemoveRange(userPgs);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

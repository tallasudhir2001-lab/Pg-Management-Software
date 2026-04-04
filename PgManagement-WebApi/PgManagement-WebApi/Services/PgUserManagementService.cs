using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.PgUser;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class PgUserManagementService : IPgUserManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly string[] AllowedRoles = ["Owner", "Manager", "Staff"];

        public PgUserManagementService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        private async Task<List<string>> GetBranchPgIds(string pgId, string? branchId)
        {
            if (string.IsNullOrEmpty(branchId)) return [pgId];
            return await _context.PGs
                .Where(p => p.BranchId == branchId)
                .Select(p => p.PgId)
                .ToListAsync();
        }

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

        public async Task<List<BranchPgInfoDto>> GetBranchPgsAsync(string pgId, string? branchId)
        {
            var pgIds = await GetBranchPgIds(pgId, branchId);
            return await _context.PGs
                .Where(p => pgIds.Contains(p.PgId))
                .Select(p => new BranchPgInfoDto { PgId = p.PgId, Name = p.Name })
                .ToListAsync();
        }

        public async Task<List<PgUserDto>> GetUsersAsync(string pgId, string? branchId, bool isOwnerOrAdmin)
        {
            var scopePgIds = isOwnerOrAdmin ? await GetBranchPgIds(pgId, branchId) : new List<string> { pgId };
            var branchPgIds = isOwnerOrAdmin ? scopePgIds : await GetBranchPgIds(pgId, branchId);

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

            return result;
        }

        public async Task<(bool success, object result, int statusCode)> AddUserAsync(
            string pgId, string? branchId, AddPgUserDto dto)
        {
            if (!AllowedRoles.Contains(dto.RoleName))
                return (false, "Invalid role. Must be Owner, Manager, or Staff.", 400);

            var branchPgIds = await GetBranchPgIds(pgId, branchId);

            var targetPgIds = dto.PgIds.Any()
                ? dto.PgIds.Intersect(branchPgIds).ToList()
                : [pgId];

            if (!targetPgIds.Any())
                return (false, "No valid PG IDs provided within this branch.", 400);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                if (string.IsNullOrEmpty(dto.Password))
                    return (false, "Password is required when creating a new user.", 400);

                user = new ApplicationUser
                {
                    UserName = SanitiseUserName(dto.Email),
                    FullName = dto.Name,
                    Email = dto.Email,
                    EmailConfirmed = true
                };
                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                    return (false, createResult.Errors.Select(e => e.Description), 400);
            }

            var existingRoles = await _userManager.GetRolesAsync(user);
            var nonAdminRoles = existingRoles.Where(r => r != "Admin").ToList();
            if (!nonAdminRoles.Any())
            {
                await _userManager.AddToRoleAsync(user, dto.RoleName);
            }

            foreach (var targetPgId in targetPgIds)
            {
                var alreadyIn = await _context.UserPgs.AnyAsync(up => up.UserId == user.Id && up.PgId == targetPgId);
                if (!alreadyIn)
                    _context.UserPgs.Add(new UserPg { UserId = user.Id, PgId = targetPgId });
            }

            await _context.SaveChangesAsync();

            return (true, await BuildUserDto(user, branchPgIds), 200);
        }

        public async Task<(bool success, object result, int statusCode)> UpdateRoleAsync(
            string userId, string pgId, string? branchId, UpdateUserRoleDto dto)
        {
            if (!AllowedRoles.Contains(dto.RoleName))
                return (false, "Invalid role. Must be Owner, Manager, or Staff.", 400);

            var branchPgIds = await GetBranchPgIds(pgId, branchId);
            var inBranch = await _context.UserPgs.AnyAsync(up => up.UserId == userId && branchPgIds.Contains(up.PgId));
            if (!inBranch) return (false, "Not found", 404);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return (false, "Not found", 404);

            var currentRoles = await _userManager.GetRolesAsync(user);
            var nonAdminRoles = currentRoles.Where(r => r != "Admin").ToList();
            if (nonAdminRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, nonAdminRoles);
            await _userManager.AddToRoleAsync(user, dto.RoleName);

            return (true, "OK", 204);
        }

        public async Task<(bool success, object result, int statusCode)> UpdatePgAssignmentsAsync(
            string userId, string pgId, string? branchId, UpdateUserPgsDto dto)
        {
            var branchPgIds = await GetBranchPgIds(pgId, branchId);
            var inBranch = await _context.UserPgs.AnyAsync(up => up.UserId == userId && branchPgIds.Contains(up.PgId));
            if (!inBranch) return (false, "Not found", 404);

            var validTargetPgIds = dto.PgIds.Intersect(branchPgIds).ToList();
            if (!validTargetPgIds.Any())
                return (false, "At least one valid PG must be assigned.", 400);

            var existingAssignments = await _context.UserPgs
                .Where(up => up.UserId == userId && branchPgIds.Contains(up.PgId))
                .ToListAsync();
            _context.UserPgs.RemoveRange(existingAssignments);

            foreach (var targetPgId in validTargetPgIds)
                _context.UserPgs.Add(new UserPg { UserId = userId, PgId = targetPgId });

            await _context.SaveChangesAsync();
            return (true, "OK", 204);
        }

        public async Task<(bool success, object result, int statusCode)> RemoveUserAsync(
            string userId, string pgId, string? branchId)
        {
            var branchPgIds = await GetBranchPgIds(pgId, branchId);
            var userPgs = await _context.UserPgs
                .Where(up => up.UserId == userId && branchPgIds.Contains(up.PgId))
                .ToListAsync();

            if (!userPgs.Any()) return (false, "Not found", 404);

            _context.UserPgs.RemoveRange(userPgs);
            await _context.SaveChangesAsync();

            return (true, "OK", 204);
        }
    }
}

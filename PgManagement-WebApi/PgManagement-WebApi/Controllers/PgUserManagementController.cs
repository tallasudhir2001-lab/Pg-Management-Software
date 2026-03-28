using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        private string? PgId => User.FindFirst("pgId")?.Value;
        private string? UserRole => User.FindFirst("role")?.Value;
        private bool IsOwnerOrAdmin => UserRole == "Owner" || User.IsInRole("Admin");

        // GET /api/pg-users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            if (!IsOwnerOrAdmin) return Forbid();

            var userPgs = await _context.UserPgs
                .Where(up => up.PgId == PgId)
                .Include(up => up.User)
                .ToListAsync();

            var result = new List<PgUserDto>();
            foreach (var up in userPgs)
            {
                var roles = await _userManager.GetRolesAsync(up.User);
                result.Add(new PgUserDto
                {
                    UserId = up.UserId,
                    Name = up.User.UserName ?? string.Empty,
                    Email = up.User.Email ?? string.Empty,
                    Role = roles.FirstOrDefault(r => r != "Admin") ?? string.Empty
                });
            }

            return Ok(result);
        }

        // POST /api/pg-users
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] AddPgUserDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            if (!IsOwnerOrAdmin) return Forbid();

            if (!AllowedRoles.Contains(dto.RoleName))
                return BadRequest("Invalid role. Must be Owner, Manager, or Staff.");

            // Find or create the user
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = dto.Name.Replace(" ", "_"),
                    Email = dto.Email,
                    EmailConfirmed = true
                };
                var createResult = await _userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                    return BadRequest(createResult.Errors.Select(e => e.Description));
            }

            // Check if already in this PG
            var alreadyInPg = await _context.UserPgs.AnyAsync(up => up.UserId == user.Id && up.PgId == PgId);
            if (alreadyInPg)
                return Conflict("User is already a member of this PG.");

            // Add to PG
            _context.UserPgs.Add(new UserPg { UserId = user.Id, PgId = PgId! });

            // Update role (replace any existing non-admin role)
            var existingRoles = await _userManager.GetRolesAsync(user);
            var nonAdminRoles = existingRoles.Where(r => r != "Admin").ToList();
            if (nonAdminRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, nonAdminRoles);
            await _userManager.AddToRoleAsync(user, dto.RoleName);

            await _context.SaveChangesAsync();

            return Ok(new PgUserDto
            {
                UserId = user.Id,
                Name = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = dto.RoleName
            });
        }

        // PUT /api/pg-users/{userId}/role
        [HttpPut("{userId}/role")]
        public async Task<IActionResult> UpdateRole(string userId, [FromBody] UpdateUserRoleDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            if (!IsOwnerOrAdmin) return Forbid();

            if (!AllowedRoles.Contains(dto.RoleName))
                return BadRequest("Invalid role. Must be Owner, Manager, or Staff.");

            var inPg = await _context.UserPgs.AnyAsync(up => up.UserId == userId && up.PgId == PgId);
            if (!inPg) return NotFound();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var nonAdminRoles = currentRoles.Where(r => r != "Admin").ToList();
            if (nonAdminRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, nonAdminRoles);
            await _userManager.AddToRoleAsync(user, dto.RoleName);

            return NoContent();
        }

        // DELETE /api/pg-users/{userId}
        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveUser(string userId)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            if (!IsOwnerOrAdmin) return Forbid();

            var userPg = await _context.UserPgs.FirstOrDefaultAsync(up => up.UserId == userId && up.PgId == PgId);
            if (userPg == null) return NotFound();

            _context.UserPgs.Remove(userPg);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}

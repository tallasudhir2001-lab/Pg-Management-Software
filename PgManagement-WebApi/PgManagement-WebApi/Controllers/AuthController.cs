using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Auth;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PgManagement_WebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;

        public AuthController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            ApplicationUser user;
            if (request.UserNameOrEmail.Contains("@"))
                user = await userManager.FindByEmailAsync(request.UserNameOrEmail);
            else
                user = await userManager.FindByNameAsync(request.UserNameOrEmail);

            if (user == null)
                return Unauthorized("Invalid credentials");

            var isValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!isValid)
                return Unauthorized("Invalid credentials");

            var isAdmin = await userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                var adminToken = GenerateAdminJwt(user);
                var adminRefresh = await CreateRefreshToken(user.Id, pgId: null);
                return Ok(new { isAdmin = true, token = adminToken, refreshToken = adminRefresh });
            }

            var userRoles = await userManager.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault(r => r != "Admin") ?? "";

            var userPgList = await context.UserPgs
                .Where(up => up.UserId == user.Id)
                .Include(up => up.PG)
                .ToListAsync();

            var pgs = userPgList.Select(up => new PgSelectionDto
            {
                PgId = up.PgId,
                PgName = up.PG.Name,
                Role = roleName
            }).ToList();

            if (pgs.Count == 0)
                return Unauthorized("No PG access configured for this account. Contact your administrator.");

            if (pgs.Count == 1)
            {
                var userPg = userPgList.First();
                var token = await GenerateTenantJwt(user, userPg);
                var refreshToken = await CreateRefreshToken(user.Id, userPg.PgId);
                return Ok(new { token, refreshToken });
            }

            var tempToken = GenerateTempJwt(user);
            return Ok(new { requirespgSelection = true, tempToken, pgs });
        }

        [HttpPost("select-pg")]
        [Authorize]
        public async Task<IActionResult> SelectPg(SelectPgDto selectedPg)
        {
            var authLevel = User.FindFirst("auth_level")?.Value;
            if (authLevel != "identity_only")
                return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userPg = await context.UserPgs
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PgId == selectedPg.PgId);
            if (userPg == null)
                return Forbid();

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized();

            var token = await GenerateTenantJwt(user, userPg);
            var refreshToken = await CreateRefreshToken(user.Id, userPg.PgId);
            return Ok(new { token, refreshToken });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var stored = await context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && !rt.IsRevoked);

            if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token.");

            // Revoke the used token (rotation — each refresh token is single-use)
            stored.IsRevoked = true;

            string newAccessToken;
            string newRefreshToken;

            if (stored.PgId == null)
            {
                newAccessToken = GenerateAdminJwt(stored.User);
                newRefreshToken = await CreateRefreshToken(stored.UserId, pgId: null);
            }
            else
            {
                var userPg = await context.UserPgs
                    .FirstOrDefaultAsync(up => up.UserId == stored.UserId && up.PgId == stored.PgId);

                if (userPg == null)
                    return Unauthorized("PG access has been revoked.");

                newAccessToken = await GenerateTenantJwt(stored.User, userPg);
                newRefreshToken = await CreateRefreshToken(stored.UserId, stored.PgId);
            }

            return Ok(new { token = newAccessToken, refreshToken = newRefreshToken });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            var stored = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && !rt.IsRevoked);

            if (stored != null)
            {
                stored.IsRevoked = true;
                await context.SaveChangesAsync();
            }

            return NoContent();
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private async Task<string> CreateRefreshToken(string userId, string? pgId)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(tokenBytes);

            context.RefreshTokens.Add(new RefreshToken
            {
                Token = token,
                UserId = userId,
                PgId = pgId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
            return token;
        }

        private string GenerateTempJwt(ApplicationUser user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("auth_level", "identity_only")
            };
            return CreateToken(claims, TimeSpan.FromMinutes(5));
        }

        private async Task<string> GenerateTenantJwt(ApplicationUser user, UserPg userPg)
        {
            var roles = await userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault(r => r != "Admin") ?? "";

            var identityRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            var roleId = identityRole?.Id ?? "";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("pgId", userPg.PgId),
                new Claim("role", roleName),
                new Claim("roleId", roleId),
                new Claim("auth_level", "tenant")
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var permissionKeys = await context.RoleAccessPoints
                .Where(rap => rap.RoleId == roleId)
                .Include(rap => rap.AccessPoint)
                .Where(rap => rap.AccessPoint.IsActive)
                .Select(rap => rap.AccessPoint.Key)
                .ToListAsync();

            claims.Add(new Claim("permissions", JsonSerializer.Serialize(permissionKeys)));

            return CreateToken(claims, TimeSpan.FromHours(2));
        }

        private string GenerateAdminJwt(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("auth_level", "admin")
            };
            return CreateToken(claims, TimeSpan.FromHours(4));
        }

        private string CreateToken(IEnumerable<Claim> claims, TimeSpan expiry)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.Add(expiry),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

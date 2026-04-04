using Microsoft.AspNetCore.Identity;
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

namespace PgManagement_WebApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<(bool success, object result, int statusCode)> LoginAsync(LoginRequestDto request)
        {
            ApplicationUser user;
            if (request.UserNameOrEmail.Contains("@"))
                user = await _userManager.FindByEmailAsync(request.UserNameOrEmail);
            else
                user = await _userManager.FindByNameAsync(request.UserNameOrEmail);

            if (user == null)
                return (false, "Invalid credentials", 401);

            var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValid)
                return (false, "Invalid credentials", 401);

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            if (isAdmin)
            {
                var adminToken = GenerateAdminJwt(user);
                var adminRefresh = await CreateRefreshToken(user.Id, pgId: null);
                return (true, new { isAdmin = true, token = adminToken, refreshToken = adminRefresh }, 200);
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault(r => r != "Admin") ?? "";

            var userPgList = await _context.UserPgs
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
                return (false, "No PG access configured for this account. Contact your administrator.", 401);

            if (pgs.Count == 1)
            {
                var userPg = userPgList.First();
                var token = await GenerateTenantJwt(user, userPg);
                var refreshToken = await CreateRefreshToken(user.Id, userPg.PgId);
                return (true, new { token, refreshToken }, 200);
            }

            var tempToken = GenerateTempJwt(user);
            return (true, new { requirespgSelection = true, tempToken, pgs }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> SelectPgAsync(string userId, SelectPgDto selectedPg)
        {
            var userPg = await _context.UserPgs
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PgId == selectedPg.PgId);
            if (userPg == null)
                return (false, "Forbidden", 403);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return (false, "Unauthorized", 401);

            var token = await GenerateTenantJwt(user, userPg);
            var refreshToken = await CreateRefreshToken(user.Id, userPg.PgId);
            return (true, new { token, refreshToken }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> RefreshAsync(RefreshTokenRequestDto dto)
        {
            var stored = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == dto.RefreshToken && !rt.IsRevoked);

            if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
                return (false, "Invalid or expired refresh token.", 401);

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
                var userPg = await _context.UserPgs
                    .FirstOrDefaultAsync(up => up.UserId == stored.UserId && up.PgId == stored.PgId);

                if (userPg == null)
                    return (false, "PG access has been revoked.", 401);

                newAccessToken = await GenerateTenantJwt(stored.User, userPg);
                newRefreshToken = await CreateRefreshToken(stored.UserId, stored.PgId);
            }

            return (true, new { token = newAccessToken, refreshToken = newRefreshToken }, 200);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var stored = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (stored != null)
            {
                stored.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────────────

        private async Task<string> CreateRefreshToken(string userId, string? pgId)
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(64);
            var token = Convert.ToBase64String(tokenBytes);

            _context.RefreshTokens.Add(new RefreshToken
            {
                Token = token,
                UserId = userId,
                PgId = pgId,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
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
            var roles = await _userManager.GetRolesAsync(user);
            var roleName = roles.FirstOrDefault(r => r != "Admin") ?? "";

            var identityRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            var roleId = identityRole?.Id ?? "";

            var pg = await _context.PGs.FindAsync(userPg.PgId);
            var branchId = pg?.BranchId ?? "";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("pgId", userPg.PgId),
                new Claim("branchId", branchId),
                new Claim("role", roleName),
                new Claim("roleId", roleId),
                new Claim("auth_level", "tenant")
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var permissionKeys = await _context.RoleAccessPoints
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.Add(expiry),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

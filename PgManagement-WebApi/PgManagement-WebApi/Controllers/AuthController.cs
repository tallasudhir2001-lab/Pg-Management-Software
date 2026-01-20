using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
using System.Text;

namespace PgManagement_WebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        public AuthController(ApplicationDbContext context,UserManager<ApplicationUser> userManager,IConfiguration configuration)
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
                return Ok(new
                {
                    isAdmin = true,
                    token = adminToken
                });
            }

            // Fetch PGs user has access to
            var pgs = await context.UserPgs
                .Where(up => up.UserId == user.Id)
                .Include(up => up.PG)
                .Include(up => up.Role)
                .Select(up => new PgSelectionDto
                {
                    PgId = up.PgId,
                    PgName = up.PG.Name,
                    Role = up.Role.Name
                }).ToListAsync();
            if (pgs.Count == 1)
            {
                var userPg = await context.UserPgs
                    .Include(x => x.Role)
                    .FirstAsync(x => x.UserId == user.Id);
                var token = await GenerateTenantJwt(user, userPg);
                return Ok(new { token });   
            }

            var tempToken = GenerateTempJwt(user);
            return Ok(new
            {
                requirespgSelection=true,
                tempToken,
                pgs
            });
        }
        [HttpPost("select-pg")]
        [Authorize]
        public async Task<IActionResult> SelectPg(SelectPgDto selectedPg)
        {
            var authLevel = User.FindFirst("auth_level")?.Value;
            if (authLevel != "identity_only")
                return Forbid();

            //how does this know user? we generate a temptoken which when there are multiple pgs, from token read it
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userPg = await context.UserPgs
                .Include(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.PgId == selectedPg.PgId);
            if (userPg == null)
                return Forbid();

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized();        

            var finalToken = GenerateTenantJwt(user, userPg);
            return Ok(new { token = finalToken });
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
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("pgId", userPg.PgId),
                new Claim("role", userPg.Role.Name),
                new Claim("auth_level", "tenant")
            };
            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return CreateToken(claims, TimeSpan.FromHours(2));
        }

        private string CreateToken(IEnumerable<Claim> claims, TimeSpan expiry)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.Add(expiry),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
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

        private string GenerateJwtToken(ApplicationUser user, PgSelectionDto pg)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("pgId", pg.PgId),
                new Claim("role", pg.Role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    
    }
}

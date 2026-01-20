using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Auth;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles ="Admin")]
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
        [HttpPost]
        [Route("register-pg")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterPg(PgRegisterRequestDto request)
        {
            var user = await userManager.FindByEmailAsync(request.OwnerEmail);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = request.OwnerName,
                    Email = request.OwnerEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);
            }

            var pg = new PG
            {
                PgId = Guid.NewGuid().ToString(),
                Name = request.PgName,
                Address = request.Address,
                ContactNumber = request.ContactNumber
            };
            context.PGs.Add(pg);

            var ownerRole = await context.PgRoles
                .FirstAsync(r => r.Name == "Owner");
            var userPg = new UserPg
            {
                UserId = user.Id,
                PgId = pg.PgId,
                RoleId = ownerRole.RoleId
            };
            context.UserPgs.Add(userPg);
            await context.SaveChangesAsync();

            return Ok(new PgResisterResponseDto
            {
                UserId = user.Id,
                PgId = pg.PgId,
            });
        }
    }
}

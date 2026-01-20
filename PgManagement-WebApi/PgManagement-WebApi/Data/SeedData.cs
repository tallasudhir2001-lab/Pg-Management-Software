using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            context.Database.Migrate();

            if (!await context.PaymentModes.AnyAsync())
            {
                var modes = new List<PaymentMode>
                {
                    new PaymentMode { Code = "CASH", Description = "Cash Payment" },
                    new PaymentMode { Code = "UPI", Description = "UPI Transfer" },
                    new PaymentMode { Code = "BANK", Description = "Bank Transfer" }
                };
                await context.PaymentModes.AddRangeAsync(modes);
                await context.SaveChangesAsync();
            }

            if(!await context.PgRoles.AnyAsync())
            {
                var roles = new List<PgRole>
                {
                    new PgRole { Name = "Owner" },
                    new PgRole { Name = "Manager" },
                    new PgRole { Name = "Staff" }
                };
                await context.PgRoles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }
            //adding admin role
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            //creating admin user
            var adminEmail = "admin@pgapp.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

        }

    }
}

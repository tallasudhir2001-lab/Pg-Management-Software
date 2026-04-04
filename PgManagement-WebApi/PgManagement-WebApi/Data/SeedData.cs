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

            // Payment Modes
            await SeedIfMissingAsync(context.PaymentModes, m => m.Code, new List<PaymentMode>
            {
                new PaymentMode { Code = "CASH", Description = "Cash Payment" },
                new PaymentMode { Code = "UPI", Description = "UPI Transfer" },
                new PaymentMode { Code = "BANK", Description = "Bank Transfer" }
            });
            await context.SaveChangesAsync();

            // Payment Frequencies
            await SeedIfMissingAsync(context.PaymentFrequencies, f => f.Code, new List<PaymentFrequency>
            {
                new PaymentFrequency { Code = "MONTHLY", Description = "Monthly", RequiresUnitCount = true },
                new PaymentFrequency { Code = "DAILY", Description = "Daily", RequiresUnitCount = true },
                new PaymentFrequency { Code = "CUSTOM", Description = "Custom Period", RequiresUnitCount = false },
                new PaymentFrequency { Code = "ONETIME", Description = "One Time", RequiresUnitCount = false }
            });
            await context.SaveChangesAsync();

            // Roles
            foreach (var roleName in new[] { "Owner", "Manager", "Staff", "Admin" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Payment Types
            await SeedIfMissingAsync(context.PaymentTypes, t => t.Code, new List<PaymentType>
            {
                new PaymentType { Code = "RENT", Name = "Rent Payment" },
                new PaymentType { Code = "ADVANCE_PAYMENT", Name = "Advance Payment" },
                new PaymentType { Code = "ADVANCE_REFUND", Name = "Advance Refund" }
            });
            await context.SaveChangesAsync();

            // Expense Categories
            await SeedIfMissingAsync(context.ExpenseCategories, c => c.Name, new List<ExpenseCategory>
            {
                new ExpenseCategory { Name = "Electricity", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Water", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Rent", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Internet", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Maintenance", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Salary", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Repairs", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Housekeeping", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Groceries", IsActive = true, CreatedAt = DateTime.UtcNow },
                new ExpenseCategory { Name = "Others", IsActive = true, CreatedAt = DateTime.UtcNow }
            });
            await context.SaveChangesAsync();

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
            }
            // Ensure Admin role is assigned even if the user already existed
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                await userManager.AddToRoleAsync(adminUser, "Admin");

        }

        private static async Task SeedIfMissingAsync<TEntity, TKey>(
            DbSet<TEntity> dbSet,
            Func<TEntity, TKey> keySelector,
            List<TEntity> seedItems) where TEntity : class
        {
            var existingKeys = await dbSet.Select(e => keySelector(e)).ToListAsync();
            var missing = seedItems.Where(item => !existingKeys.Contains(keySelector(item))).ToList();
            if (missing.Count > 0)
                await dbSet.AddRangeAsync(missing);
        }

    }
}

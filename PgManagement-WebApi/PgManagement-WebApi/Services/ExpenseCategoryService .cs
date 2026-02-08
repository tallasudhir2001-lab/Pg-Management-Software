using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Expense;

namespace PgManagement_WebApi.Services
{
    public class ExpenseCategoryService : IExpenseCategoryService
    {
        private readonly ApplicationDbContext context;

        public ExpenseCategoryService(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<List<ExpenseCategoryDto>> GetActiveCategoriesAsync()
        {
            return await context.ExpenseCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ExpenseCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
        }
    }

}

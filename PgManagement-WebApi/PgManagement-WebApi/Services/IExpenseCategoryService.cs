using PgManagement_WebApi.DTOs.Expense;

namespace PgManagement_WebApi.Services
{
    public interface IExpenseCategoryService
    {
        Task<List<ExpenseCategoryDto>> GetActiveCategoriesAsync();
    }
}

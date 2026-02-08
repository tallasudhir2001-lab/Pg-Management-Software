using PgManagement_WebApi.DTOs.Expense;
using PgManagement_WebApi.DTOs.Pagination;

namespace PgManagement_WebApi.Services
{
    public interface IExpenseService
    {
        Task<PageResultsDto<ExpenseListItemDto>> GetExpensesAsync(string pgId,ExpenseListQueryDto query);
        Task<ExpenseSummaryDto> GetExpenseSummaryAsync(string pgId, DateTime? fromDate, DateTime? toDate, int? categoryId);
        Task<ExpenseDetailsDto?> GetExpenseByIdAsync(string pgId, string id);
        Task<string> CreateExpenseAsync(string pgId,CreateExpenseDto dto);
        Task UpdateExpenseAsync(string expenseId, UpdateExpenseDto dto);
        Task DeleteExpenseAsync(string expenseId);

    }
}

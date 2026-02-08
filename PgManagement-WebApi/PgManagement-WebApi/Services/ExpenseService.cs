using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Expense;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PageResultsDto<ExpenseListItemDto>> GetExpensesAsync(string pgId,
            ExpenseListQueryDto query)
        {
            var expensesQuery = _context.Expenses
                .AsNoTracking()
                .Where(e => e.PgId == pgId);

            // -------------------------
            // Filters
            // -------------------------
            if (query.FromDate.HasValue)
                expensesQuery = expensesQuery
                    .Where(e => e.ExpenseDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                expensesQuery = expensesQuery
                    .Where(e => e.ExpenseDate <= query.ToDate.Value);

            if (query.CategoryId.HasValue)
                expensesQuery = expensesQuery
                    .Where(e => e.CategoryId == query.CategoryId.Value);

            if (query.MinAmount.HasValue)
                expensesQuery = expensesQuery
                    .Where(e => e.Amount >= query.MinAmount.Value);

            if (query.MaxAmount.HasValue)
                expensesQuery = expensesQuery
                    .Where(e => e.Amount <= query.MaxAmount.Value);

            // -------------------------
            // Total count
            // -------------------------
            var totalCount = await expensesQuery.CountAsync();

            // -------------------------
            // Sorting
            // -------------------------
            expensesQuery = query.SortBy?.ToLower() switch
            {
                "amount" => query.SortDir == "asc"
                    ? expensesQuery.OrderBy(e => e.Amount)
                    : expensesQuery.OrderByDescending(e => e.Amount),

                "expensedate" => query.SortDir == "asc"
                    ? expensesQuery.OrderBy(e => e.ExpenseDate)
                    : expensesQuery.OrderByDescending(e => e.CreatedAt),

                _ => expensesQuery.OrderByDescending(e => e.CreatedAt)
            };

            // -------------------------
            // Pagination + Projection
            // -------------------------
            var items = await expensesQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(e => new ExpenseListItemDto
                {
                    Id = e.Id,
                    ExpenseDate = e.ExpenseDate,
                    Category = e.Category.Name,
                    Amount = e.Amount,
                    PaymentMode = e.PaymentMode.Code,
                    PaymentModeLabel = e.PaymentMode.Description,
                    Description = e.Description
                })
                .ToListAsync();

            return new PageResultsDto<ExpenseListItemDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
        public async Task<ExpenseSummaryDto> GetExpenseSummaryAsync(string pgId, DateTime? fromDate, DateTime? toDate, int? categoryId)
        {
            var query = _context.Expenses
                .AsNoTracking()
                .Where(e => e.PgId == pgId);

            // -------------------------
            // Date filters
            // -------------------------
            if (fromDate.HasValue)
                query = query.Where(e => e.ExpenseDate.Date >= fromDate.Value.Date);

            if (toDate.HasValue)
                query = query.Where(e => e.ExpenseDate.Date <= toDate.Value.Date);

            //category filter
            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            // -------------------------
            // Total expense
            // -------------------------
            var totalExpense = await query.SumAsync(e => e.Amount);

            // -------------------------
            // Category-wise breakdown
            // -------------------------
            var categoryBreakdown = await query
                .GroupBy(e => new { e.CategoryId, e.Category.Name })
                .Select(g => new ExpenseCategorySummaryDto
                {
                    CategoryId = g.Key.CategoryId,
                    Category = g.Key.Name,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            return new ExpenseSummaryDto
            {
                TotalExpense = totalExpense,
                CategoryBreakdown = categoryBreakdown
            };
        }
        public async Task<ExpenseDetailsDto?> GetExpenseByIdAsync(string pgId, string id)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Where(e => e.Id == id && e.PgId == pgId)
                .Select(e => new ExpenseDetailsDto
                {
                    Id = e.Id,
                    CategoryId = e.CategoryId,
                    Amount = e.Amount,
                    ExpenseDate = e.ExpenseDate,
                    Description = e.Description,
                    PaymentModeCode = e.PaymentModeCode,
                    ReferenceNo = e.ReferenceNo,
                    IsRecurring = e.IsRecurring,
                    RecurringFrequency = e.RecurringFrequency
                })
                .FirstOrDefaultAsync();
        }

        public async Task<string> CreateExpenseAsync(string pgId, CreateExpenseDto dto)
        {
            var expense = new Expense
            {
                Id = Guid.NewGuid().ToString(),
                PgId = pgId,
                CategoryId = dto.CategoryId,
                Amount = dto.Amount,
                ExpenseDate = dto.ExpenseDate,
                Description = dto.Description,
                PaymentModeCode = dto.PaymentModeCode,
                ReferenceNo = dto.ReferenceNo,
                IsRecurring = dto.IsRecurring,
                RecurringFrequency = dto.RecurringFrequency
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return expense.Id;
        }
        public async Task UpdateExpenseAsync(string expenseId, UpdateExpenseDto dto)
        {
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (expense == null)
                throw new KeyNotFoundException("Expense not found.");

            expense.CategoryId = dto.CategoryId;
            expense.Amount = dto.Amount;
            expense.ExpenseDate = dto.ExpenseDate;
            expense.Description = dto.Description;
            expense.PaymentModeCode = dto.PaymentModeCode;
            expense.ReferenceNo = dto.ReferenceNo;
            expense.IsRecurring = dto.IsRecurring;
            expense.RecurringFrequency = dto.RecurringFrequency;

            await _context.SaveChangesAsync();
        }
        public async Task DeleteExpenseAsync(string expenseId)
        {
            var expense = await _context.Expenses
                .FirstOrDefaultAsync(e => e.Id == expenseId);

            if (expense == null)
                throw new KeyNotFoundException("Expense not found.");

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
        }

       

    }
}

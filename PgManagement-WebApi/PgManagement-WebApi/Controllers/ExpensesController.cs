using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Expense;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/expenses")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IExpenseService _expenseService;

        public ExpensesController(ApplicationDbContext context, IExpenseService expenseService)
        {
            _context = context;
            _expenseService = expenseService;
        }

        [AccessPoint("Expense", "View All Expenses")]
        [HttpGet]
        public async Task<IActionResult> GetExpenses([FromQuery] ExpenseListQueryDto query)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _expenseService.GetExpensesAsync(pgIds, query));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetExpenseSummary(
            [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int? categoryId)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _expenseService.GetExpenseSummaryAsync(pgIds, fromDate, toDate, categoryId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var expense = await _expenseService.GetExpenseByIdAsync(pgId, id);
            if (expense == null) return NotFound();
            return Ok(expense);
        }

        [AccessPoint("Expense", "Create Expense")]
        [HttpPost("create-expense")]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var id = await _expenseService.CreateExpenseAsync(pgId, dto, branchId);
            return CreatedAtAction(nameof(GetExpenses), new { id }, null);
        }

        [AccessPoint("Expense", "Update Expense")]
        [HttpPut("update-expense/{id}")]
        public async Task<IActionResult> UpdateExpense(string id, [FromBody] UpdateExpenseDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _expenseService.UpdateExpenseAsync(id, dto, pgId, userId);
            return NoContent();
        }

        [AccessPoint("Expense", "Delete Expense")]
        [HttpDelete("delete-expense/{id}")]
        public async Task<IActionResult> DeleteExpense(string id)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _expenseService.DeleteExpenseAsync(id, pgId, userId);
            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Expense;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/expenses")]
    [ApiController]
    public class ExpensesController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        private readonly IExpenseService expenseService;
        public ExpensesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, IExpenseService expenseService)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
            this.expenseService = expenseService;
        }

        [AccessPoint("Expense", "View All Expenses")]
        [HttpGet]
        public async Task<IActionResult> GetExpenses(
        [FromQuery] ExpenseListQueryDto query)
        {
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var result = await expenseService.GetExpensesAsync(pgIds, query);
            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetExpenseSummary([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int? categoryId)
        {
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var summary = await expenseService
                .GetExpenseSummaryAsync(pgIds, fromDate, toDate, categoryId);

            return Ok(summary);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();
            var expense = await expenseService.GetExpenseByIdAsync(pgId, id);
            if (expense == null) return NotFound();
            return Ok(expense);
        }

        [AccessPoint("Expense", "Create Expense")]
        [HttpPost("create-expense")]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var id = await expenseService.CreateExpenseAsync(pgId, dto, branchId);
            return CreatedAtAction(nameof(GetExpenses), new { id }, null);
        }

        [AccessPoint("Expense", "Update Expense")]
        [HttpPut("update-expense/{id}")]
        public async Task<IActionResult> UpdateExpense(string id,[FromBody] UpdateExpenseDto dto)
        {
            await expenseService.UpdateExpenseAsync(id, dto);
            return NoContent();
        }

        [AccessPoint("Expense", "Delete Expense")]
        [HttpDelete("delete-expense/{id}")]
        public async Task<IActionResult> DeleteExpense(string id)
        {
            await expenseService.DeleteExpenseAsync(id);
            return NoContent();
        }
    }
}

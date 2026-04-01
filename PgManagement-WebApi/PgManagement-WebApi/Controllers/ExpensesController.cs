using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Expense;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;
using PgManagement_WebApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Load old amount before update for audit
            if (!string.IsNullOrEmpty(pgId) && !string.IsNullOrEmpty(userId))
            {
                var existing = await context.Expenses.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id && e.PgId == pgId);

                if (existing != null && existing.Amount != dto.Amount)
                {
                    context.AuditEvents.Add(new AuditEvent
                    {
                        Id = Guid.NewGuid().ToString(),
                        PgId = pgId,
                        BranchId = existing.BranchId,
                        EventType = "EXPENSE_AMOUNT_CHANGED",
                        EntityType = "Expense",
                        EntityId = id,
                        Description = $"Expense amount changed from ₹{existing.Amount} to ₹{dto.Amount}",
                        OldValue = System.Text.Json.JsonSerializer.Serialize(new { existing.Amount }),
                        NewValue = System.Text.Json.JsonSerializer.Serialize(new { dto.Amount }),
                        PerformedByUserId = userId,
                        PerformedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }

            await expenseService.UpdateExpenseAsync(id, dto);
            return NoContent();
        }

        [AccessPoint("Expense", "Delete Expense")]
        [HttpDelete("delete-expense/{id}")]
        public async Task<IActionResult> DeleteExpense(string id)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(pgId) && !string.IsNullOrEmpty(userId))
            {
                var existing = await context.Expenses.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id && e.PgId == pgId);

                if (existing != null)
                {
                    context.AuditEvents.Add(new AuditEvent
                    {
                        Id = Guid.NewGuid().ToString(),
                        PgId = pgId,
                        BranchId = existing.BranchId,
                        EventType = "EXPENSE_DELETED",
                        EntityType = "Expense",
                        EntityId = id,
                        Description = $"Expense of ₹{existing.Amount} deleted",
                        OldValue = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            existing.Amount,
                            existing.ExpenseDate,
                            existing.Description,
                            existing.CategoryId
                        }),
                        PerformedByUserId = userId,
                        PerformedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }

            await expenseService.DeleteExpenseAsync(id);
            return NoContent();
        }
    }
}

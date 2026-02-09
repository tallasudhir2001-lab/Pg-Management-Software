using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Dashboard;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public DashboardController(ApplicationDbContext context)
        {
            this.context = context;
        }

        // ----------------------------------------------------
        // 1️⃣ DASHBOARD SUMMARY
        // ----------------------------------------------------
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
    DateTime? from,
    DateTime? to)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var now = DateTime.UtcNow;
            var start = from ?? new DateTime(now.Year, now.Month, 1);
            var end = to ?? now;

            // Rooms (no date relevance)
            var totalRooms = await context.Rooms
                .AsNoTracking()
                .CountAsync(r => r.PgId == pgId);

            // Tenants (no date relevance)
            var totalTenants = await context.Tenants
                .AsNoTracking()
                .CountAsync(t => t.PgId == pgId && !t.isDeleted);

            var activeTenants = await context.TenantRooms
                .AsNoTracking()
                .Where(tr => tr.PgId == pgId && tr.ToDate == null)
                .Select(tr => tr.TenantId)
                .Distinct()
                .CountAsync();

            var movedOutTenants = totalTenants - activeTenants;

            // Beds
            var totalBeds = await context.Rooms
                .AsNoTracking()
                .Where(r => r.PgId == pgId)
                .SumAsync(r => r.Capacity);

            var occupiedBeds = await context.TenantRooms
                .AsNoTracking()
                .CountAsync(tr => tr.PgId == pgId && tr.ToDate == null);

            // ✅ Revenue filtered by date range
            var revenue = await context.Payments
                .AsNoTracking()
                .Where(p =>
                    p.PgId == pgId &&
                    !p.IsDeleted &&
                    p.PaymentDate >= start &&
                    p.PaymentDate <= end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Ok(new DashboardSummaryDto
            {
                TotalRooms = totalRooms,
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                MovedOutTenants = movedOutTenants,
                OccupiedBeds = occupiedBeds,
                VacantBeds = totalBeds - occupiedBeds,
                MonthlyRevenue = revenue
            });
        }


        // ----------------------------------------------------
        // 2️⃣ REVENUE TREND (YEARLY)
        // ----------------------------------------------------
        [HttpGet("revenue-trend")]
        public async Task<IActionResult> GetRevenueTrend(
    DateTime? from,
    DateTime? to)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var start = from ?? DateTime.UtcNow.AddMonths(-11);
            var end = to ?? DateTime.UtcNow;

            var data = await context.Payments
                .AsNoTracking()
                .Where(p =>
                    p.PgId == pgId &&
                    !p.IsDeleted &&
                    p.PaymentDate >= start &&
                    p.PaymentDate <= end)
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .Select(g => new RevenueTrendDto
                {
                    Month = g.Key.Month,
                    Amount = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            return Ok(data);
        }


        // ----------------------------------------------------
        // 3️⃣ RECENT PAYMENTS
        // ----------------------------------------------------
        [HttpGet("recent-payments")]
        public async Task<IActionResult> GetRecentPayments(
    int limit = 5,
    DateTime? from = null,
    DateTime? to = null)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var query = context.Payments
                .AsNoTracking()
                .Include(p => p.Tenant)
                .Where(p =>
                    p.PgId == pgId &&
                    !p.IsDeleted);

            if (from.HasValue)
                query = query.Where(p => p.PaymentDate >= from.Value);

            if (to.HasValue)
                query = query.Where(p => p.PaymentDate <= to.Value);

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Take(limit)
                .Select(p => new RecentPaymentDto
                {
                    TenantName = p.Tenant.Name,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Mode = p.PaymentModeCode
                })
                .ToListAsync();

            return Ok(payments);
        }


        // ----------------------------------------------------
        // 4️⃣ OCCUPANCY SNAPSHOT
        // ----------------------------------------------------
        [HttpGet("occupancy")]
        public async Task<IActionResult> GetOccupancy()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var totalBeds = await context.Rooms
                .AsNoTracking()
                .Where(r => r.PgId == pgId)
                .SumAsync(r => r.Capacity);

            var occupiedBeds = await context.TenantRooms
                .AsNoTracking()
                .CountAsync(tr =>
                    tr.PgId == pgId &&
                    tr.ToDate == null);

            return Ok(new
            {
                Occupied = occupiedBeds,
                Vacant = totalBeds - occupiedBeds
            });
        }

        [HttpGet("expenses-summary")]
        public async Task<IActionResult> GetExpensesSummary(
    DateTime? from,
    DateTime? to)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var now = DateTime.UtcNow;
            var start = from ?? new DateTime(now.Year, now.Month, 1);
            var end = to ?? now;

            var totalExpenses = await context.Expenses
                .AsNoTracking()
                .Where(e =>
                    e.PgId == pgId &&
                    !e.IsDeleted &&
                    e.ExpenseDate >= start &&
                    e.ExpenseDate <= end)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            return Ok(new DashboardExpenseSummaryDto
            {
                TotalExpenses = totalExpenses
            });
        }

    }
}

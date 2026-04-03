using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Dashboard;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Models;

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
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var now = DateTime.UtcNow;
            var start = from ?? new DateTime(now.Year, now.Month, 1);
            var end = to ?? now;

            // Rooms (no date relevance)
            var totalRooms = await context.Rooms
                .AsNoTracking()
                .CountAsync(r => pgIds.Contains(r.PgId));

            // Tenants (no date relevance)
            var totalTenants = await context.Tenants
                .AsNoTracking()
                .CountAsync(t => pgIds.Contains(t.PgId) && !t.isDeleted);

            var activeTenants = await context.TenantRooms
                .AsNoTracking()
                .Where(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null)
                .Select(tr => tr.TenantId)
                .Distinct()
                .CountAsync();

            var movedOutTenants = totalTenants - activeTenants;

            // Beds
            var totalBeds = await context.Rooms
                .AsNoTracking()
                .Where(r => pgIds.Contains(r.PgId))
                .SumAsync(r => r.Capacity);

            var occupiedBeds = await context.TenantRooms
                .AsNoTracking()
                .CountAsync(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null);

            // ✅ Revenue filtered by date range
            var revenue = await context.Payments
                .AsNoTracking()
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
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
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var start = from ?? DateTime.UtcNow.AddMonths(-11);
            var end = to ?? DateTime.UtcNow;

            var data = await context.Payments
                .AsNoTracking()
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
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
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var query = context.Payments
                .AsNoTracking()
                .Include(p => p.Tenant)
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
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
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var totalBeds = await context.Rooms
                .AsNoTracking()
                .Where(r => pgIds.Contains(r.PgId))
                .SumAsync(r => r.Capacity);

            var occupiedBeds = await context.TenantRooms
                .AsNoTracking()
                .CountAsync(tr =>
                    pgIds.Contains(tr.PgId) &&
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
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var now = DateTime.UtcNow;
            var start = from ?? new DateTime(now.Year, now.Month, 1);
            var end = to ?? now;

            var totalExpenses = await context.Expenses
                .AsNoTracking()
                .Where(e =>
                    pgIds.Contains(e.PgId) &&
                    !e.IsDeleted &&
                    e.ExpenseDate >= start &&
                    e.ExpenseDate <= end)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            return Ok(new DashboardExpenseSummaryDto
            {
                TotalExpenses = totalExpenses
            });
        }

        // ----------------------------------------------------
        // 6️⃣ ALERTS (counts for toast banners)
        // ----------------------------------------------------
        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var today = DateTime.UtcNow.Date;

            // Fetch all tenant-room stays
            var tenantRooms = await context.TenantRooms
                .AsNoTracking()
                .Where(tr => pgIds.Contains(tr.PgId))
                .Select(tr => new { tr.TenantId, tr.FromDate, tr.ToDate })
                .ToListAsync();

            var activeTenantIds = tenantRooms
                .Where(tr => tr.ToDate == null)
                .Select(tr => tr.TenantId)
                .Distinct()
                .ToHashSet();

            var allTenantIdsWithStay = tenantRooms
                .Select(tr => tr.TenantId)
                .Distinct()
                .ToList();

            var movedOutTenantIds = allTenantIdsWithStay
                .Where(id => !activeTenantIds.Contains(id))
                .ToList();

            // Fetch non-deleted, non-advance payments for these tenants
            var payments = await context.Payments
                .AsNoTracking()
                .Where(p =>
                    allTenantIdsWithStay.Contains(p.TenantId) &&
                    !p.IsDeleted &&
                    p.PaymentTypeCode != "ADVANCE_PAYMENT")
                .Select(p => new { p.TenantId, PaidFrom = p.PaidFrom.Date, PaidUpto = p.PaidUpto.Date })
                .ToListAsync();

            var paymentsLookup = payments
                .GroupBy(p => p.TenantId)
                .ToDictionary(g => g.Key, g => g.Select(p => (p.PaidFrom, p.PaidUpto)).ToList());

            var staysLookup = tenantRooms
                .GroupBy(tr => tr.TenantId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Helper: check if a tenant has any unpaid rent period
            bool HasPendingRent(string tenantId)
            {
                if (!staysLookup.TryGetValue(tenantId, out var stays)) return false;
                var tenantPayments = paymentsLookup.TryGetValue(tenantId, out var tp)
                    ? tp
                    : new List<(DateTime, DateTime)>();

                foreach (var stay in stays)
                {
                    var stayFrom = stay.FromDate.Date;
                    var stayTo = stay.ToDate?.Date ?? today;
                    var unpaid = DateRangeHelper.Subtract(stayFrom, stayTo, tenantPayments);
                    if (unpaid.Any()) return true;
                }
                return false;
            }

            // 1) Moved out + pending rent
            var movedOutWithPendingRent = movedOutTenantIds.Count(HasPendingRent);

            // 2) Moved out + unsettled advance
            var movedOutWithUnsettledAdvance = await context.Advances
                .AsNoTracking()
                .Where(a => movedOutTenantIds.Contains(a.TenantId) && !a.IsSettled && !a.IsDeleted)
                .Select(a => a.TenantId)
                .Distinct()
                .CountAsync();

            // 3) Active + pending rent
            var activeWithPendingRent = activeTenantIds.Count(HasPendingRent);

            // 4) Overdue expected checkouts — active tenants past their expected checkout date
            int overdueExpectedCheckouts = 0;
            try
            {
                overdueExpectedCheckouts = await context.TenantRooms
                    .AsNoTracking()
                    .CountAsync(tr =>
                        pgIds.Contains(tr.PgId) &&
                        tr.ToDate == null &&
                        tr.ExpectedCheckOutDate != null &&
                        tr.ExpectedCheckOutDate.Value < today);
            }
            catch { /* Column may not exist yet if migration hasn't run */ }

            // 5) Overdue bookings — active bookings past their scheduled check-in date
            var overdueBookings = await context.Bookings
                .AsNoTracking()
                .CountAsync(b =>
                    pgIds.Contains(b.PgId) &&
                    b.Status == BookingStatus.Active &&
                    !b.IsDeleted &&
                    b.ScheduledCheckInDate.Date < today);

            return Ok(new DashboardAlertsDto
            {
                MovedOutWithPendingRent = movedOutWithPendingRent,
                MovedOutWithUnsettledAdvance = movedOutWithUnsettledAdvance,
                ActiveWithPendingRent = activeWithPendingRent,
                OverdueExpectedCheckouts = overdueExpectedCheckouts,
                OverdueBookings = overdueBookings
            });
        }

        // ----------------------------------------------------
        // 7️⃣ COLLECTION SUMMARY
        // ----------------------------------------------------
        [HttpGet("collection-summary")]
        public async Task<IActionResult> GetCollectionSummary(
            DateTime? from,
            DateTime? to)
        {
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var today = DateTime.UtcNow.Date;
            var start = from?.Date ?? new DateTime(today.Year, today.Month, 1);
            var end = to?.Date ?? today;

            // Get active tenants (those with an open stay)
            var activeTenantIds = await context.TenantRooms
                .AsNoTracking()
                .Where(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null)
                .Select(tr => tr.TenantId)
                .Distinct()
                .ToListAsync();

            if (!activeTenantIds.Any())
            {
                return Ok(new CollectionSummaryDto());
            }

            // Get current rent amounts for active tenants
            var tenantRents = await context.TenantRentHistories
                .AsNoTracking()
                .Where(trh => activeTenantIds.Contains(trh.TenantId) && trh.ToDate == null)
                .Include(trh => trh.RoomRentHistory)
                .Select(trh => new { trh.TenantId, trh.RoomRentHistory.RentAmount })
                .ToListAsync();

            var expectedRent = tenantRents.Sum(r => r.RentAmount);

            // Get payments in this period
            var periodPayments = await context.Payments
                .AsNoTracking()
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
                    !p.IsDeleted &&
                    activeTenantIds.Contains(p.TenantId) &&
                    p.PaymentTypeCode != "ADVANCE_PAYMENT" &&
                    p.PaymentDate >= start &&
                    p.PaymentDate <= end)
                .GroupBy(p => p.TenantId)
                .Select(g => new { TenantId = g.Key, Total = g.Sum(p => p.Amount) })
                .ToListAsync();

            var collectedRent = periodPayments.Sum(p => p.Total);
            var paidTenantIds = periodPayments.Select(p => p.TenantId).ToHashSet();
            var paidCount = paidTenantIds.Count;
            var pendingCount = activeTenantIds.Count - paidCount;
            var pendingRent = expectedRent - collectedRent;
            if (pendingRent < 0) pendingRent = 0;

            var collectionRate = expectedRent > 0
                ? Math.Round((double)(collectedRent / expectedRent) * 100, 1)
                : 0;

            return Ok(new CollectionSummaryDto
            {
                ExpectedRent = expectedRent,
                CollectedRent = collectedRent,
                PendingRent = pendingRent,
                CollectionRate = collectionRate,
                PaidCount = paidCount,
                PendingCount = pendingCount
            });
        }

    }
}

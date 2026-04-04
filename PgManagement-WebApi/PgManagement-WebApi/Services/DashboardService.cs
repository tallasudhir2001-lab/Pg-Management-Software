using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Dashboard;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryDto> GetSummaryAsync(List<string> pgIds, DateTime? from, DateTime? to)
        {
            var now = DateTime.UtcNow;
            var start = from ?? new DateTime(now.Year, now.Month, 1);
            var end = to ?? now;

            var totalRooms = await _context.Rooms
                .AsNoTracking()
                .CountAsync(r => pgIds.Contains(r.PgId));

            var totalTenants = await _context.Tenants
                .AsNoTracking()
                .CountAsync(t => pgIds.Contains(t.PgId) && !t.isDeleted);

            var activeTenants = await _context.TenantRooms
                .AsNoTracking()
                .Where(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null)
                .Select(tr => tr.TenantId)
                .Distinct()
                .CountAsync();

            var movedOutTenants = totalTenants - activeTenants;

            var totalBeds = await _context.Rooms
                .AsNoTracking()
                .Where(r => pgIds.Contains(r.PgId))
                .SumAsync(r => r.Capacity);

            var occupiedBeds = await _context.TenantRooms
                .AsNoTracking()
                .CountAsync(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null);

            var revenue = await _context.Payments
                .AsNoTracking()
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
                    !p.IsDeleted &&
                    p.PaymentDate >= start &&
                    p.PaymentDate <= end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return new DashboardSummaryDto
            {
                TotalRooms = totalRooms,
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                MovedOutTenants = movedOutTenants,
                OccupiedBeds = occupiedBeds,
                VacantBeds = totalBeds - occupiedBeds,
                MonthlyRevenue = revenue
            };
        }

        public async Task<List<RevenueTrendDto>> GetRevenueTrendAsync(List<string> pgIds, DateTime? from, DateTime? to)
        {
            var start = from ?? DateTime.UtcNow.AddMonths(-11);
            var end = to ?? DateTime.UtcNow;

            return await _context.Payments
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
        }

        public async Task<List<RecentPaymentDto>> GetRecentPaymentsAsync(
            List<string> pgIds, int limit, DateTime? from, DateTime? to)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Tenant)
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
                    !p.IsDeleted);

            if (from.HasValue)
                query = query.Where(p => p.PaymentDate >= from.Value);

            if (to.HasValue)
                query = query.Where(p => p.PaymentDate <= to.Value);

            return await query
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
        }

        public async Task<object> GetOccupancyAsync(List<string> pgIds)
        {
            var totalBeds = await _context.Rooms
                .AsNoTracking()
                .Where(r => pgIds.Contains(r.PgId))
                .SumAsync(r => r.Capacity);

            var occupiedBeds = await _context.TenantRooms
                .AsNoTracking()
                .CountAsync(tr =>
                    pgIds.Contains(tr.PgId) &&
                    tr.ToDate == null);

            return new
            {
                Occupied = occupiedBeds,
                Vacant = totalBeds - occupiedBeds
            };
        }

        public async Task<DashboardExpenseSummaryDto> GetExpensesSummaryAsync(
            List<string> pgIds, DateTime? from, DateTime? to)
        {
            var now = DateTime.UtcNow;
            var start = from ?? new DateTime(now.Year, now.Month, 1);
            var end = to ?? now;

            var totalExpenses = await _context.Expenses
                .AsNoTracking()
                .Where(e =>
                    pgIds.Contains(e.PgId) &&
                    !e.IsDeleted &&
                    e.ExpenseDate >= start &&
                    e.ExpenseDate <= end)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            return new DashboardExpenseSummaryDto
            {
                TotalExpenses = totalExpenses
            };
        }

        public async Task<DashboardAlertsDto> GetAlertsAsync(List<string> pgIds)
        {
            var today = DateTime.UtcNow.Date;

            var tenantRooms = await _context.TenantRooms
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

            var payments = await _context.Payments
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

            var movedOutWithPendingRent = movedOutTenantIds.Count(HasPendingRent);

            var movedOutWithUnsettledAdvance = await _context.Advances
                .AsNoTracking()
                .Where(a => movedOutTenantIds.Contains(a.TenantId) && !a.IsSettled && !a.IsDeleted)
                .Select(a => a.TenantId)
                .Distinct()
                .CountAsync();

            var activeWithPendingRent = activeTenantIds.Count(HasPendingRent);

            int overdueExpectedCheckouts = 0;
            try
            {
                overdueExpectedCheckouts = await _context.TenantRooms
                    .AsNoTracking()
                    .CountAsync(tr =>
                        pgIds.Contains(tr.PgId) &&
                        tr.ToDate == null &&
                        tr.ExpectedCheckOutDate != null &&
                        tr.ExpectedCheckOutDate.Value < today);
            }
            catch { /* Column may not exist yet if migration hasn't run */ }

            var overdueBookings = await _context.Bookings
                .AsNoTracking()
                .CountAsync(b =>
                    pgIds.Contains(b.PgId) &&
                    b.Status == BookingStatus.Active &&
                    !b.IsDeleted &&
                    b.ScheduledCheckInDate.Date < today);

            return new DashboardAlertsDto
            {
                MovedOutWithPendingRent = movedOutWithPendingRent,
                MovedOutWithUnsettledAdvance = movedOutWithUnsettledAdvance,
                ActiveWithPendingRent = activeWithPendingRent,
                OverdueExpectedCheckouts = overdueExpectedCheckouts,
                OverdueBookings = overdueBookings
            };
        }

        public async Task<CollectionSummaryDto> GetCollectionSummaryAsync(
            List<string> pgIds, DateTime? from, DateTime? to)
        {
            var today = DateTime.UtcNow.Date;
            var start = from?.Date ?? new DateTime(today.Year, today.Month, 1);
            var end = to?.Date ?? today;

            var activeTenantIds = await _context.TenantRooms
                .AsNoTracking()
                .Where(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null)
                .Select(tr => tr.TenantId)
                .Distinct()
                .ToListAsync();

            if (!activeTenantIds.Any())
            {
                return new CollectionSummaryDto();
            }

            var tenantRents = await _context.TenantRentHistories
                .AsNoTracking()
                .Where(trh => activeTenantIds.Contains(trh.TenantId) && trh.ToDate == null)
                .Include(trh => trh.RoomRentHistory)
                .Select(trh => new { trh.TenantId, trh.RoomRentHistory.RentAmount })
                .ToListAsync();

            var expectedRent = tenantRents.Sum(r => r.RentAmount);

            var periodPayments = await _context.Payments
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

            return new CollectionSummaryDto
            {
                ExpectedRent = expectedRent,
                CollectedRent = collectedRent,
                PendingRent = pendingRent,
                CollectionRate = collectionRate,
                PaidCount = paidCount,
                PendingCount = pendingCount
            };
        }
    }
}

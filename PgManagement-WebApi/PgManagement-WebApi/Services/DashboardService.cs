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

            var salaryOutflow = await _context.SalaryPayments
                .AsNoTracking()
                .Where(sp =>
                    pgIds.Contains(sp.PgId) &&
                    !sp.IsDeleted &&
                    sp.PaymentDate >= start &&
                    sp.PaymentDate <= end)
                .SumAsync(sp => (decimal?)sp.Amount) ?? 0;

            return new DashboardSummaryDto
            {
                TotalRooms = totalRooms,
                TotalTenants = totalTenants,
                ActiveTenants = activeTenants,
                MovedOutTenants = movedOutTenants,
                OccupiedBeds = occupiedBeds,
                VacantBeds = totalBeds - occupiedBeds,
                MonthlyRevenue = revenue,
                MonthlySalaryOutflow = salaryOutflow
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

            var activeStays = await _context.TenantRooms
                .AsNoTracking()
                .Where(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null)
                .ToListAsync();

            var activeTenantIds = activeStays.Select(s => s.TenantId).Distinct().ToList();

            if (!activeTenantIds.Any())
            {
                return new CollectionSummaryDto();
            }

            // Get all rent payments for active tenants (excluding advances)
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
                    !p.IsDeleted &&
                    activeTenantIds.Contains(p.TenantId) &&
                    p.PaymentTypeCode == "RENT")
                .ToListAsync();

            var paymentsByTenant = payments
                .GroupBy(p => p.TenantId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Get rent histories for all rooms occupied by active tenants
            var roomIds = activeStays.Select(s => s.RoomId).Distinct().ToList();
            var rentHistories = await _context.RoomRentHistories
                .AsNoTracking()
                .Where(rr => roomIds.Contains(rr.RoomId))
                .OrderBy(rr => rr.EffectiveFrom)
                .ToListAsync();

            var rentByRoom = rentHistories
                .GroupBy(rr => rr.RoomId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Calculate total pending rent across all unpaid periods
            decimal totalPending = 0;

            foreach (var stay in activeStays)
            {
                var stayFrom = stay.FromDate.Date;
                var stayTo = today;

                var tenantPayments = paymentsByTenant.TryGetValue(stay.TenantId, out var tp)
                    ? tp.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date))
                    : Enumerable.Empty<(DateTime, DateTime)>();

                var unpaidRanges = DateRangeHelper.Subtract(stayFrom, stayTo, tenantPayments);
                if (!unpaidRanges.Any()) continue;

                var roomRents = rentByRoom.GetValueOrDefault(stay.RoomId, new List<RoomRentHistory>());

                foreach (var range in unpaidRanges)
                {
                    var slices = RentHelper.GetRentSlices(
                        range.From, range.To, roomRents, stay.StayType,
                        stayFromDate: stay.FromDate.Date, isActiveStay: true);

                    totalPending += slices.Sum(s => s.Amount);
                }
            }

            // Collected rent in the selected date range
            var periodPayments = payments
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
                .ToList();

            var collectedRent = periodPayments.Sum(p => p.Amount);
            var paidTenantIds = periodPayments.Select(p => p.TenantId).Distinct().ToHashSet();
            var paidCount = paidTenantIds.Count;

            // Pending count = tenants who still have any unpaid rent
            var tenantsWithPending = new HashSet<string>();
            foreach (var stay in activeStays)
            {
                var tenantPayments = paymentsByTenant.TryGetValue(stay.TenantId, out var tp)
                    ? tp.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date))
                    : Enumerable.Empty<(DateTime, DateTime)>();

                var unpaidRanges = DateRangeHelper.Subtract(stay.FromDate.Date, today, tenantPayments);
                if (unpaidRanges.Any())
                    tenantsWithPending.Add(stay.TenantId);
            }

            var pendingCount = tenantsWithPending.Count;
            var expectedRent = totalPending + collectedRent;

            var collectionRate = expectedRent > 0
                ? Math.Round((double)(collectedRent / expectedRent) * 100, 1)
                : 0;

            return new CollectionSummaryDto
            {
                ExpectedRent = expectedRent,
                CollectedRent = collectedRent,
                PendingRent = totalPending,
                CollectionRate = collectionRate,
                PaidCount = paidCount,
                PendingCount = pendingCount
            };
        }

        public async Task<VacancyLossDto> GetVacancyLossAsync(List<string> pgIds)
        {
            var rooms = await _context.Rooms
                .AsNoTracking()
                .Where(r => pgIds.Contains(r.PgId))
                .Select(r => new { r.RoomId, r.RoomNumber, r.Capacity })
                .ToListAsync();

            var occupiedCounts = await _context.TenantRooms
                .AsNoTracking()
                .Where(tr => pgIds.Contains(tr.PgId) && tr.ToDate == null)
                .GroupBy(tr => tr.RoomId)
                .Select(g => new { RoomId = g.Key, Count = g.Count() })
                .ToListAsync();

            var occupiedLookup = occupiedCounts.ToDictionary(x => x.RoomId, x => x.Count);

            var currentRents = await _context.RoomRentHistories
                .AsNoTracking()
                .Where(rr => rooms.Select(r => r.RoomId).Contains(rr.RoomId) && rr.EffectiveTo == null)
                .Select(rr => new { rr.RoomId, rr.RentAmount })
                .ToListAsync();

            var rentLookup = currentRents.ToDictionary(x => x.RoomId, x => x.RentAmount);

            var roomDtos = new List<VacancyLossRoomDto>();

            foreach (var room in rooms)
            {
                var occupied = occupiedLookup.GetValueOrDefault(room.RoomId, 0);
                var vacant = room.Capacity - occupied;
                if (vacant <= 0) continue;

                var rentPerBed = rentLookup.GetValueOrDefault(room.RoomId, 0);
                if (rentPerBed <= 0) continue;

                roomDtos.Add(new VacancyLossRoomDto
                {
                    RoomId = room.RoomId,
                    RoomNumber = room.RoomNumber,
                    Capacity = room.Capacity,
                    Occupied = occupied,
                    VacantBeds = vacant,
                    RentPerBed = rentPerBed,
                    MonthlyLoss = vacant * rentPerBed
                });
            }

            var totalBeds = rooms.Sum(r => r.Capacity);

            return new VacancyLossDto
            {
                TotalMonthlyLoss = roomDtos.Sum(r => r.MonthlyLoss),
                TotalVacantBeds = roomDtos.Sum(r => r.VacantBeds),
                TotalBeds = totalBeds,
                Rooms = roomDtos.OrderByDescending(r => r.MonthlyLoss).ToList()
            };
        }

        public async Task<TodaySnapshotDto> GetTodaySnapshotAsync(List<string> pgIds)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var todayCollection = await _context.Payments
                .AsNoTracking()
                .Where(p =>
                    pgIds.Contains(p.PgId) &&
                    !p.IsDeleted &&
                    p.PaymentDate >= today &&
                    p.PaymentDate < tomorrow)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var todayExpenses = await _context.Expenses
                .AsNoTracking()
                .Where(e =>
                    pgIds.Contains(e.PgId) &&
                    !e.IsDeleted &&
                    e.ExpenseDate >= today &&
                    e.ExpenseDate < tomorrow)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

            var todaySalaries = await _context.SalaryPayments
                .AsNoTracking()
                .Where(sp =>
                    pgIds.Contains(sp.PgId) &&
                    !sp.IsDeleted &&
                    sp.PaymentDate >= today &&
                    sp.PaymentDate < tomorrow)
                .SumAsync(sp => (decimal?)sp.Amount) ?? 0;

            return new TodaySnapshotDto
            {
                TodayCollection = todayCollection,
                TodayExpenses = todayExpenses,
                TodaySalaries = todaySalaries
            };
        }
    }
}

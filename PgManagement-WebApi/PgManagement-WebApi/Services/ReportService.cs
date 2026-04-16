using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Models;
using PgManagement_WebApi.Options;
using PgManagement_WebApi.Reports;
using QuestPDF.Fluent;

namespace PgManagement_WebApi.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        private async Task<PgProfileOptions> GetPgProfileAsync(string pgId)
        {
            var pg = await _db.PGs.FindAsync(pgId);
            return new PgProfileOptions
            {
                Name = pg?.Name ?? "PG Management",
                Address = pg?.Address ?? "",
                Phone = pg?.ContactNumber ?? ""
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // RECEIPT
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateReceiptAsync(string paymentId, string pgId)
        {
            var data = await BuildReceiptDataAsync(paymentId, pgId);
            var pgProfile = await GetPgProfileAsync(pgId);
            var doc = new PaymentReceiptDocument(data, pgProfile);
            return doc.GeneratePdf();
        }

        public async Task<ReceiptDataDto> BuildReceiptDataAsync(string paymentId, string pgId)
        {
            var payment = await _db.Payments
                .Include(p => p.Tenant)
                .Include(p => p.PaymentMode)
                .Include(p => p.PaymentType)
                .Where(p => p.PaymentId == paymentId && p.PgId == pgId && !p.IsDeleted)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Payment not found");

            // Find room at payment date
            var room = await _db.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr =>
                    tr.TenantId == payment.TenantId &&
                    tr.PgId == pgId &&
                    tr.FromDate <= payment.PaymentDate &&
                    (tr.ToDate == null || tr.ToDate >= payment.PaymentDate))
                .OrderByDescending(tr => tr.FromDate)
                .FirstOrDefaultAsync();

            var receiptNum = $"REC-{payment.PaymentId[^6..].ToUpper()}";

            return new ReceiptDataDto
            {
                PaymentId = payment.PaymentId,
                ReceiptNumber = receiptNum,
                PaymentDate = payment.PaymentDate,
                Amount = payment.Amount,
                PaymentType = payment.PaymentType?.Name ?? payment.PaymentTypeCode,
                PaymentMode = payment.PaymentMode?.Description ?? payment.PaymentModeCode,
                PaidFrom = payment.PaidFrom,
                PaidUpto = payment.PaidUpto,
                Notes = payment.Notes,
                TenantName = payment.Tenant.Name,
                TenantPhone = payment.Tenant.ContactNumber,
                TenantEmail = payment.Tenant.Email,
                RoomNumber = room?.Room?.RoomNumber
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // RENT COLLECTION
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateRentCollectionReportAsync(string pgId, int month, int year, string? roomId, string? status)
        {
            var data = await GetRentCollectionDataAsync(pgId, month, year, roomId, status);
            return new RentCollectionDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<RentCollectionReportDto> GetRentCollectionDataAsync(string pgId, int month, int year, string? roomId, string? status)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Get all tenants with stays covering this month
            var staysQuery = _db.TenantRooms
                .Include(tr => tr.Room)
                .Include(tr => tr.Tenant)
                .Where(tr =>
                    tr.PgId == pgId &&
                    tr.FromDate <= monthEnd &&
                    (tr.ToDate == null || tr.ToDate >= monthStart));

            if (!string.IsNullOrEmpty(roomId))
                staysQuery = staysQuery.Where(tr => tr.RoomId == roomId);

            var stays = await staysQuery.ToListAsync();

            var rows = new List<RentCollectionRowDto>();

            foreach (var stay in stays)
            {
                var stayFrom = stay.FromDate.Date;
                var stayEnd = stay.ToDate?.Date ?? monthEnd;

                var periodFrom = Max(stayFrom, monthStart);
                var periodTo = Min(stayEnd, monthEnd);

                if (periodFrom > periodTo) continue;

                // Get rent histories
                var rentHistories = await _db.RoomRentHistories
                    .Where(r =>
                        r.RoomId == stay.RoomId &&
                        r.EffectiveFrom <= periodTo &&
                        (r.EffectiveTo == null || r.EffectiveTo >= periodFrom))
                    .OrderBy(r => r.EffectiveFrom)
                    .ToListAsync();

                var slices = RentHelper.GetRentSlices(periodFrom, periodTo, rentHistories, stay.StayType,
                    stayFromDate: stay.FromDate.Date, isActiveStay: stay.ToDate == null);
                var expectedRent = slices.Sum(s => s.Amount);

                // Get RENT payments for this tenant in this month range
                var tenantPayments = await _db.Payments
                    .Where(p =>
                        p.TenantId == stay.TenantId &&
                        p.PgId == pgId &&
                        !p.IsDeleted &&
                        p.PaymentTypeCode == "RENT" &&
                        p.PaidFrom <= monthEnd &&
                        p.PaidUpto >= monthStart)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                var amountPaid = tenantPayments.Sum(p => p.Amount);
                var lastPayment = tenantPayments.FirstOrDefault();

                var rowStatus = amountPaid >= expectedRent && expectedRent > 0 ? "Paid"
                    : amountPaid > 0 ? "Partial"
                    : "Overdue";

                rows.Add(new RentCollectionRowDto
                {
                    RoomNumber = stay.Room.RoomNumber,
                    TenantName = stay.Tenant.Name,
                    TenantPhone = stay.Tenant.ContactNumber,
                    ExpectedRent = Math.Round(expectedRent, 2),
                    AmountPaid = amountPaid,
                    LastPaymentDate = lastPayment?.PaymentDate,
                    PaymentMode = lastPayment?.PaymentModeCode,
                    Status = rowStatus
                });
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
                rows = rows.Where(r => r.Status == status).ToList();

            return new RentCollectionReportDto
            {
                Month = month,
                Year = year,
                Rows = rows.OrderBy(r => r.RoomNumber).ToList(),
                TotalExpected = rows.Sum(r => r.ExpectedRent),
                TotalCollected = rows.Sum(r => r.AmountPaid),
                TotalPending = rows.Sum(r => Math.Max(0, r.ExpectedRent - r.AmountPaid))
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // OVERDUE RENT
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateOverdueRentReportAsync(string pgId, DateTime asOfDate, string? roomId)
        {
            var data = await GetOverdueRentDataAsync(pgId, asOfDate, roomId);
            return new OverdueRentDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<OverdueRentReportDto> GetOverdueRentDataAsync(string pgId, DateTime asOfDate, string? roomId)
        {
            var asOf = asOfDate.Date;

            var staysQuery = _db.TenantRooms
                .Include(tr => tr.Room)
                .Include(tr => tr.Tenant)
                .Where(tr => tr.PgId == pgId && tr.ToDate == null); // active stays only

            if (!string.IsNullOrEmpty(roomId))
                staysQuery = staysQuery.Where(tr => tr.RoomId == roomId);

            var activeStays = await staysQuery.ToListAsync();
            var rows = new List<OverdueRentRowDto>();

            foreach (var stay in activeStays)
            {
                var stayFrom = stay.FromDate.Date;

                var payments = await _db.Payments
                    .Where(p =>
                        p.TenantId == stay.TenantId &&
                        p.PgId == pgId &&
                        !p.IsDeleted &&
                        p.PaymentTypeCode == "RENT")
                    .OrderByDescending(p => p.PaidUpto)
                    .ToListAsync();

                var unpaid = DateRangeHelper.Subtract(
                    stayFrom, asOf,
                    payments.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date)));

                if (!unpaid.Any()) continue;

                var rentHistories = await _db.RoomRentHistories
                    .Where(r =>
                        r.RoomId == stay.RoomId &&
                        r.EffectiveFrom <= asOf &&
                        (r.EffectiveTo == null || r.EffectiveTo >= stayFrom))
                    .OrderBy(r => r.EffectiveFrom)
                    .ToListAsync();

                decimal outstanding = 0;
                foreach (var range in unpaid)
                {
                    var slices = RentHelper.GetRentSlices(range.From, range.To, rentHistories, stay.StayType,
                        stayFromDate: stay.FromDate.Date, isActiveStay: true); // overdue report is active stays only
                    outstanding += slices.Sum(s => s.Amount);
                }

                var lastPayment = payments.FirstOrDefault();
                var overdueSince = unpaid.OrderBy(r => r.From).First().From;

                rows.Add(new OverdueRentRowDto
                {
                    RoomNumber = stay.Room.RoomNumber,
                    TenantName = stay.Tenant.Name,
                    TenantPhone = stay.Tenant.ContactNumber,
                    LastPaymentDate = lastPayment?.PaymentDate,
                    PaidUpTo = lastPayment?.PaidUpto,
                    OverdueSince = overdueSince,
                    DaysOverdue = (int)(asOf - overdueSince).TotalDays,
                    OutstandingAmount = Math.Round(outstanding, 2)
                });
            }

            return new OverdueRentReportDto
            {
                AsOfDate = asOf,
                Rows = rows,
                TotalOverdueTenants = rows.Count,
                TotalOutstanding = rows.Sum(r => r.OutstandingAmount)
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // PAYMENT HISTORY
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GeneratePaymentHistoryReportAsync(string pgId, DateTime fromDate, DateTime toDate, string? types, string? modes, string? tenantId)
        {
            var data = await GetPaymentHistoryDataAsync(pgId, fromDate, toDate, types, modes, tenantId);
            return new PaymentHistoryDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<PaymentHistoryReportDto> GetPaymentHistoryDataAsync(string pgId, DateTime fromDate, DateTime toDate, string? types, string? modes, string? tenantId)
        {
            var query = _db.Payments
                .Include(p => p.Tenant)
                .Include(p => p.PaymentType)
                .Include(p => p.PaymentMode)
                .Where(p =>
                    p.PgId == pgId &&
                    !p.IsDeleted &&
                    p.PaymentDate >= fromDate.Date &&
                    p.PaymentDate <= toDate.Date);

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(p => p.TenantId == tenantId);

            if (!string.IsNullOrEmpty(types))
            {
                var typeCodes = types.Split(',').Select(t => t.Trim().ToUpper()).ToList();
                query = query.Where(p => typeCodes.Contains(p.PaymentTypeCode));
            }

            if (!string.IsNullOrEmpty(modes))
            {
                var modeCodes = modes.Split(',').Select(m => m.Trim().ToLower()).ToList();
                query = query.Where(p => modeCodes.Contains(p.PaymentModeCode.ToLower()));
            }

            var payments = await query.OrderBy(p => p.PaymentDate).ToListAsync();

            // Get room per payment
            var tenantIds = payments.Select(p => p.TenantId).Distinct().ToList();
            var rooms = await _db.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tenantIds.Contains(tr.TenantId) && tr.PgId == pgId)
                .ToListAsync();

            var rows = payments.Select(p =>
            {
                var room = rooms
                    .Where(tr =>
                        tr.TenantId == p.TenantId &&
                        tr.FromDate <= p.PaymentDate &&
                        (tr.ToDate == null || tr.ToDate >= p.PaymentDate))
                    .OrderByDescending(tr => tr.FromDate)
                    .FirstOrDefault();

                return new PaymentHistoryReportRowDto
                {
                    PaymentDate = p.PaymentDate,
                    ReceiptNumber = $"REC-{p.PaymentId[^6..].ToUpper()}",
                    TenantName = p.Tenant.Name,
                    RoomNumber = room?.Room?.RoomNumber,
                    PaymentType = p.PaymentType?.Name ?? p.PaymentTypeCode,
                    PaymentMode = p.PaymentMode?.Description ?? p.PaymentModeCode,
                    Amount = p.Amount,
                    Notes = p.Notes
                };
            }).ToList();

            return new PaymentHistoryReportDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                Rows = rows,
                TotalAmount = rows.Sum(r => r.Amount)
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // OCCUPANCY
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateOccupancyReportAsync(string pgId, DateTime asOfDate)
        {
            var data = await GetOccupancyDataAsync(pgId, asOfDate);
            return new OccupancyDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<OccupancyReportDto> GetOccupancyDataAsync(string pgId, DateTime asOfDate)
        {
            var asOf = asOfDate.Date;

            var rooms = await _db.Rooms
                .Where(r => r.PgId == pgId)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            var occupiedStays = await _db.TenantRooms
                .Include(tr => tr.Tenant)
                .Where(tr =>
                    tr.PgId == pgId &&
                    tr.FromDate <= asOf &&
                    (tr.ToDate == null || tr.ToDate > asOf))
                .ToListAsync();

            var rows = rooms.Select(room =>
            {
                var tenants = occupiedStays
                    .Where(tr => tr.RoomId == room.RoomId)
                    .Select(tr => tr.Tenant.Name)
                    .ToList();

                var occupied = tenants.Count;
                var vacant = room.Capacity - occupied;

                return new OccupancyRowDto
                {
                    RoomNumber = room.RoomNumber,
                    TotalBeds = room.Capacity,
                    OccupiedBeds = occupied,
                    VacantBeds = Math.Max(0, vacant),
                    OccupancyPercent = room.Capacity > 0 ? (double)occupied / room.Capacity * 100 : 0,
                    TenantNames = string.Join(", ", tenants)
                };
            }).ToList();

            var totalBeds = rows.Sum(r => r.TotalBeds);
            var totalOccupied = rows.Sum(r => r.OccupiedBeds);

            return new OccupancyReportDto
            {
                AsOfDate = asOf,
                Rows = rows,
                TotalRooms = rooms.Count,
                TotalBeds = totalBeds,
                TotalOccupied = totalOccupied,
                TotalVacant = totalBeds - totalOccupied,
                OverallOccupancyPercent = totalBeds > 0 ? (double)totalOccupied / totalBeds * 100 : 0
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // TENANT LIST
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateTenantListReportAsync(string pgId, string status, string? roomId)
        {
            var data = await GetTenantListDataAsync(pgId, status, roomId);
            return new TenantListDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<TenantListReportDto> GetTenantListDataAsync(string pgId, string status, string? roomId)
        {
            var tenants = await _db.Tenants
                .Where(t => t.PgId == pgId && !t.isDeleted)
                .ToListAsync();

            var allStays = await _db.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.PgId == pgId)
                .ToListAsync();

            var rows = new List<TenantListRowDto>();

            foreach (var t in tenants)
            {
                var stays = allStays.Where(tr => tr.TenantId == t.TenantId).ToList();
                var activeStay = stays.FirstOrDefault(s => s.ToDate == null);
                var lastStay = stays.OrderByDescending(s => s.ToDate ?? DateTime.MaxValue).FirstOrDefault();

                var tenantStatus = activeStay != null ? "Active"
                    : stays.Any() ? "Moved Out"
                    : "No Stay";

                if (status != "All" && !string.Equals(tenantStatus, status, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(roomId) && activeStay?.RoomId != roomId)
                    continue;

                var aadhaar = t.AadharNumber?.Length >= 4
                    ? "XXXX-XXXX-" + t.AadharNumber[^4..]
                    : "—";

                // Get approximate monthly rent (30 × latest daily rate)
                var currentStay = activeStay ?? lastStay;
                decimal monthlyRent = 0;
                if (currentStay != null)
                {
                    var latestRent = await _db.RoomRentHistories
                        .Where(r => r.RoomId == currentStay.RoomId && r.EffectiveTo == null)
                        .OrderByDescending(r => r.EffectiveFrom)
                        .FirstOrDefaultAsync();

                    if (latestRent != null)
                        monthlyRent = latestRent.RentAmount;
                }

                rows.Add(new TenantListRowDto
                {
                    TenantName = t.Name,
                    Phone = t.ContactNumber,
                    AadhaarMasked = aadhaar,
                    RoomNumber = (activeStay ?? lastStay)?.Room?.RoomNumber,
                    CheckInDate = (activeStay ?? lastStay)?.FromDate,
                    MoveOutDate = lastStay?.ToDate,
                    Status = tenantStatus,
                    MonthlyRent = monthlyRent
                });
            }

            return new TenantListReportDto
            {
                StatusFilter = status,
                Rows = rows.OrderBy(r => r.RoomNumber).ToList()
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // ADVANCE BALANCE
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateAdvanceBalanceReportAsync(string pgId)
        {
            var data = await GetAdvanceBalanceDataAsync(pgId);
            return new AdvanceBalanceDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<AdvanceBalanceReportDto> GetAdvanceBalanceDataAsync(string pgId)
        {
            var advances = await _db.Advances
                .Include(a => a.Tenant)
                .Where(a => a.Tenant.PgId == pgId && !a.Tenant.isDeleted)
                .ToListAsync();

            var activeStays = await _db.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.PgId == pgId && tr.ToDate == null)
                .ToListAsync();

            var grouped = advances
                .GroupBy(a => a.TenantId)
                .Select(g =>
                {
                    var paid = g.Sum(a => a.Amount);
                    var refunded = g.Sum(a => a.DeductedAmount) ?? 0m;
                    var balance = paid - refunded;
                    var tenant = g.First().Tenant;
                    var room = activeStays.FirstOrDefault(tr => tr.TenantId == g.Key)?.Room?.RoomNumber;

                    var rowStatus = balance <= 0 ? "Fully Refunded"
                        : refunded > 0 ? "Partially Refunded"
                        : "Held";

                    return new AdvanceBalanceRowDto
                    {
                        TenantName = tenant.Name,
                        RoomNumber = room,
                        AdvancePaid = paid,
                        AdvanceRefunded = refunded,
                        Balance = balance,
                        Status = rowStatus
                    };
                }).ToList();

            return new AdvanceBalanceReportDto
            {
                Rows = grouped,
                TotalHeld = grouped.Sum(r => r.Balance > 0 ? r.Balance : 0),
                TotalRefunded = grouped.Sum(r => r.AdvanceRefunded),
                NetBalance = grouped.Sum(r => r.Balance)
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // EXPENSE REPORT
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateExpenseReportAsync(string pgId, int month, int year, string? categories)
        {
            var data = await GetExpenseReportDataAsync(pgId, month, year, categories);
            return new ExpenseReportDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<ExpenseReportDto> GetExpenseReportDataAsync(string pgId, int month, int year, string? categories)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var query = _db.Expenses
                .Include(e => e.Category)
                .Where(e =>
                    e.PgId == pgId &&
                    e.ExpenseDate >= monthStart &&
                    e.ExpenseDate <= monthEnd);

            if (!string.IsNullOrEmpty(categories))
            {
                var cats = categories.Split(',').Select(c => c.Trim()).ToList();
                query = query.Where(e => cats.Contains(e.Category.Name));
            }

            var expenses = await query.OrderBy(e => e.Category.Name).ThenBy(e => e.ExpenseDate).ToListAsync();

            var groups = expenses
                .GroupBy(e => e.Category.Name)
                .Select(g => new ExpenseCategoryGroupDto
                {
                    Category = g.Key,
                    Rows = g.Select(e => new ExpenseReportRowDto
                    {
                        Date = e.ExpenseDate,
                        Category = g.Key,
                        Description = e.Description ?? "",
                        Amount = e.Amount
                    }).ToList(),
                    Subtotal = g.Sum(e => e.Amount)
                }).ToList();

            return new ExpenseReportDto
            {
                Month = month,
                Year = year,
                Groups = groups,
                GrandTotal = groups.Sum(g => g.Subtotal)
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // PROFIT & LOSS
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateProfitLossReportAsync(string pgId, int month, int year)
        {
            var data = await GetProfitLossDataAsync(pgId, month, year);
            return new ProfitLossDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<ProfitLossReportDto> GetProfitLossDataAsync(string pgId, int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var payments = await _db.Payments
                .Where(p =>
                    p.PgId == pgId &&
                    !p.IsDeleted &&
                    p.PaymentDate >= monthStart &&
                    p.PaymentDate <= monthEnd)
                .ToListAsync();

            var rentCollected = payments.Where(p => p.PaymentTypeCode == "RENT").Sum(p => p.Amount);
            var advanceReceived = payments.Where(p => p.PaymentTypeCode == "ADVANCE_PAYMENT").Sum(p => p.Amount);
            var totalRevenue = rentCollected + advanceReceived;

            var expenses = await _db.Expenses
                .Include(e => e.Category)
                .Where(e =>
                    e.PgId == pgId &&
                    e.ExpenseDate >= monthStart &&
                    e.ExpenseDate <= monthEnd)
                .ToListAsync();

            var totalExpenses = expenses.Sum(e => e.Amount);
            var expenseByCategory = expenses
                .GroupBy(e => e.Category.Name)
                .Select(g => new ProfitLossExpenseCategoryDto { Category = g.Key, Amount = g.Sum(e => e.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // Collection efficiency: rent collected vs expected this month
            var rentData = await GetRentCollectionDataAsync(pgId, month, year, null, null);
            var expectedRent = rentData.TotalExpected;
            double? efficiency = expectedRent > 0 ? (double)(rentCollected / expectedRent * 100) : null;

            return new ProfitLossReportDto
            {
                Month = month,
                Year = year,
                TotalRentCollected = rentCollected,
                TotalAdvanceReceived = advanceReceived,
                TotalRevenue = totalRevenue,
                TotalExpenses = totalExpenses,
                ExpenseByCategory = expenseByCategory,
                NetProfitOrLoss = totalRevenue - totalExpenses,
                TotalExpectedRent = expectedRent,
                CollectionEfficiencyPercent = efficiency
            };
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        // ──────────────────────────────────────────────────────────────────────
        // TENANT TURNOVER / CHURN
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateTenantTurnoverReportAsync(string pgId, int month, int year)
        {
            var data = await GetTenantTurnoverDataAsync(pgId, month, year);
            return new TenantTurnoverDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<TenantTurnoverReportDto> GetTenantTurnoverDataAsync(string pgId, int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var stays = await _db.TenantRooms
                .Include(tr => tr.Room)
                .Include(tr => tr.Tenant)
                .Where(tr => tr.PgId == pgId)
                .ToListAsync();

            var moveIns = stays.Count(s => s.FromDate.Date >= monthStart && s.FromDate.Date <= monthEnd);

            var movedOut = stays
                .Where(s => s.ToDate.HasValue && s.ToDate.Value.Date >= monthStart && s.ToDate.Value.Date <= monthEnd)
                .ToList();

            // For churn, filter out room changes (where a consecutive stay exists for same tenant)
            var actualMoveOuts = movedOut.Where(s =>
            {
                var nextStay = stays.FirstOrDefault(ns =>
                    ns.TenantId == s.TenantId && ns.FromDate.Date == s.ToDate!.Value.Date.AddDays(1));
                return nextStay == null;
            }).ToList();

            var activeTenants = stays.Count(s => s.ToDate == null || s.ToDate.Value.Date > monthEnd);
            var churnRate = activeTenants > 0 ? (double)actualMoveOuts.Count / activeTenants * 100 : 0;

            var avgStayDays = actualMoveOuts.Any()
                ? actualMoveOuts.Average(s => (s.ToDate!.Value.Date - s.FromDate.Date).Days)
                : 0;

            return new TenantTurnoverReportDto
            {
                Month = month,
                Year = year,
                MoveIns = moveIns,
                MoveOuts = actualMoveOuts.Count,
                AverageStayDays = Math.Round(avgStayDays, 1),
                ChurnRatePercent = Math.Round(churnRate, 1),
                MoveOutDetails = actualMoveOuts.Select(s => new TurnoverRowDto
                {
                    TenantName = s.Tenant?.Name ?? "",
                    RoomNumber = s.Room?.RoomNumber ?? "",
                    CheckInDate = s.FromDate.Date,
                    CheckOutDate = s.ToDate!.Value.Date,
                    StayDays = (s.ToDate!.Value.Date - s.FromDate.Date).Days
                }).OrderBy(r => r.CheckOutDate).ToList()
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // ROOM REVENUE
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateRoomRevenueReportAsync(string pgId, int month, int year)
        {
            var data = await GetRoomRevenueDataAsync(pgId, month, year);
            return new RoomRevenueDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<RoomRevenueReportDto> GetRoomRevenueDataAsync(string pgId, int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var daysInMonth = DateTime.DaysInMonth(year, month);

            var rooms = await _db.Rooms.Where(r => r.PgId == pgId).OrderBy(r => r.RoomNumber).ToListAsync();

            var stays = await _db.TenantRooms
                .Where(tr => tr.PgId == pgId && tr.FromDate <= monthEnd && (tr.ToDate == null || tr.ToDate >= monthStart))
                .ToListAsync();

            var payments = await _db.Payments
                .Where(p => p.PgId == pgId && !p.IsDeleted && p.PaymentTypeCode == "RENT"
                    && p.PaymentDate >= monthStart && p.PaymentDate <= monthEnd)
                .ToListAsync();

            var expenses = await _db.Expenses
                .Where(e => e.PgId == pgId && e.ExpenseDate >= monthStart && e.ExpenseDate <= monthEnd)
                .ToListAsync();
            var totalExpense = expenses.Sum(e => e.Amount);
            var roomCount = rooms.Count;

            var rows = new List<RoomRevenueRowDto>();
            foreach (var room in rooms)
            {
                var roomStays = stays.Where(s => s.RoomId == room.RoomId).ToList();
                int occupiedDays = 0;
                foreach (var s in roomStays)
                {
                    var from = Max(s.FromDate.Date, monthStart);
                    var to = Min(s.ToDate?.Date ?? monthEnd, monthEnd);
                    occupiedDays += Math.Max(0, (to - from).Days + 1);
                }
                occupiedDays = Math.Min(occupiedDays, daysInMonth);

                var rentForRoom = payments
                    .Where(p => _db.TenantRooms.Local.Any(tr => tr.RoomId == room.RoomId && tr.TenantId == p.TenantId))
                    .Sum(p => p.Amount);

                // Simple equal allocation of expenses across rooms
                var expenseAlloc = roomCount > 0 ? Math.Round(totalExpense / roomCount, 2) : 0;

                rows.Add(new RoomRevenueRowDto
                {
                    RoomNumber = room.RoomNumber,
                    Capacity = room.Capacity,
                    OccupiedDays = occupiedDays,
                    VacantDays = daysInMonth - occupiedDays,
                    RentCollected = rentForRoom,
                    ExpenseAllocated = expenseAlloc,
                    NetRevenue = rentForRoom - expenseAlloc
                });
            }

            return new RoomRevenueReportDto
            {
                Month = month, Year = year, Rows = rows,
                TotalRentCollected = rows.Sum(r => r.RentCollected),
                TotalExpenses = totalExpense,
                TotalNetRevenue = rows.Sum(r => r.NetRevenue)
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // SALARY REPORT
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateSalaryReportAsync(string pgId, int month, int year)
        {
            var data = await GetSalaryReportDataAsync(pgId, month, year);
            return new SalaryReportDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<SalaryReportDto> GetSalaryReportDataAsync(string pgId, int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var salaries = await _db.SalaryPayments
                .Include(sp => sp.Employee).ThenInclude(e => e.EmployeeRole)
                .Where(sp => sp.PgId == pgId && sp.PaymentDate >= monthStart && sp.PaymentDate <= monthEnd)
                .OrderBy(sp => sp.PaymentDate)
                .ToListAsync();

            return new SalaryReportDto
            {
                Month = month, Year = year,
                Rows = salaries.Select(sp => new SalaryReportRowDto
                {
                    EmployeeName = sp.Employee?.Name ?? "",
                    Role = sp.Employee?.EmployeeRole?.Name ?? sp.Employee?.RoleCode ?? "",
                    PaymentDate = sp.PaymentDate,
                    ForMonth = sp.ForMonth,
                    Amount = sp.Amount,
                    PaymentMode = sp.PaymentModeCode
                }).ToList(),
                TotalPaid = salaries.Sum(sp => sp.Amount),
                EmployeeCount = salaries.Select(sp => sp.EmployeeId).Distinct().Count()
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // CASH FLOW STATEMENT
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateCashFlowReportAsync(string pgId, int month, int year)
        {
            var data = await GetCashFlowDataAsync(pgId, month, year);
            return new CashFlowDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<CashFlowReportDto> GetCashFlowDataAsync(string pgId, int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var payments = await _db.Payments
                .Where(p => p.PgId == pgId && !p.IsDeleted && p.PaymentDate >= monthStart && p.PaymentDate <= monthEnd)
                .ToListAsync();

            var rentInflow = payments.Where(p => p.PaymentTypeCode == "RENT").Sum(p => p.Amount);
            var advanceInflow = payments.Where(p => p.PaymentTypeCode == "ADVANCE_PAYMENT").Sum(p => p.Amount);

            var expenses = await _db.Expenses
                .Include(e => e.Category)
                .Where(e => e.PgId == pgId && e.ExpenseDate >= monthStart && e.ExpenseDate <= monthEnd)
                .ToListAsync();
            var totalExpenses = expenses.Sum(e => e.Amount);

            var salaries = await _db.SalaryPayments
                .Where(sp => sp.PgId == pgId && sp.PaymentDate >= monthStart && sp.PaymentDate <= monthEnd)
                .SumAsync(sp => sp.Amount);

            var inflows = new List<CashFlowLineDto>
            {
                new() { Label = "Rent Collected", Amount = rentInflow },
                new() { Label = "Advance Payments Received", Amount = advanceInflow }
            };
            var totalIn = inflows.Sum(i => i.Amount);

            var outflows = new List<CashFlowLineDto>();

            // Add expense categories
            var expCats = expenses.GroupBy(e => e.Category?.Name ?? "Uncategorised")
                .Select(g => new CashFlowLineDto { Label = g.Key, Amount = g.Sum(e => e.Amount) })
                .OrderByDescending(x => x.Amount).ToList();
            outflows.AddRange(expCats);

            outflows.Add(new CashFlowLineDto { Label = "Salary Payments", Amount = salaries });
            var totalOut = outflows.Sum(o => o.Amount);

            return new CashFlowReportDto
            {
                Month = month, Year = year,
                Inflows = inflows, TotalInflows = totalIn,
                Outflows = outflows, TotalOutflows = totalOut,
                NetCashFlow = totalIn - totalOut
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // TENANT AGING
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateTenantAgingReportAsync(string pgId, DateTime asOfDate)
        {
            var data = await GetTenantAgingDataAsync(pgId, asOfDate);
            return new TenantAgingDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<TenantAgingReportDto> GetTenantAgingDataAsync(string pgId, DateTime asOfDate)
        {
            var asOf = asOfDate.Date;

            var activeStays = await _db.TenantRooms
                .Include(tr => tr.Tenant)
                .Include(tr => tr.Room)
                .Where(tr => tr.PgId == pgId && tr.ToDate == null)
                .ToListAsync();

            var rentPayments = await _db.Payments
                .Where(p => p.PgId == pgId && !p.IsDeleted && p.PaymentTypeCode == "RENT")
                .ToListAsync();

            var details = new List<TenantAgingRowDto>();

            foreach (var stay in activeStays)
            {
                var lastPayment = rentPayments
                    .Where(p => p.TenantId == stay.TenantId)
                    .OrderByDescending(p => p.PaidUpto).FirstOrDefault();

                var paidUpTo = lastPayment?.PaidUpto.Date ?? stay.FromDate.Date.AddDays(-1);

                if (paidUpTo >= asOf) continue;

                int daysOverdue = (asOf - paidUpTo).Days;
                string bucket = daysOverdue switch
                {
                    <= 7 => "0–7 days",
                    <= 15 => "8–15 days",
                    <= 30 => "16–30 days",
                    _ => "30+ days"
                };

                var rentHistories = await _db.RoomRentHistories
                    .Where(r => r.RoomId == stay.RoomId && r.EffectiveFrom <= asOf
                        && (r.EffectiveTo == null || r.EffectiveTo >= paidUpTo))
                    .OrderBy(r => r.EffectiveFrom)
                    .ToListAsync();

                var slices = RentHelper.GetRentSlices(paidUpTo.AddDays(1), asOf, rentHistories, stay.StayType,
                    stayFromDate: stay.FromDate.Date, isActiveStay: true);
                var pending = slices.Sum(s => s.Amount);

                details.Add(new TenantAgingRowDto
                {
                    TenantName = stay.Tenant?.Name ?? "",
                    RoomNumber = stay.Room?.RoomNumber ?? "",
                    PendingAmount = Math.Round(pending, 2),
                    DaysOverdue = daysOverdue,
                    Bucket = bucket
                });
            }

            var buckets = details.GroupBy(d => d.Bucket)
                .Select(g => new TenantAgingBucketDto { Bucket = g.Key, Count = g.Count(), TotalAmount = g.Sum(d => d.PendingAmount) })
                .ToList();

            // Ensure all buckets present
            var allBuckets = new[] { "0–7 days", "8–15 days", "16–30 days", "30+ days" };
            foreach (var b in allBuckets.Where(b => !buckets.Any(x => x.Bucket == b)))
                buckets.Add(new TenantAgingBucketDto { Bucket = b, Count = 0, TotalAmount = 0 });

            buckets = buckets.OrderBy(b => Array.IndexOf(allBuckets, b.Bucket)).ToList();

            return new TenantAgingReportDto
            {
                AsOfDate = asOf,
                Buckets = buckets,
                Details = details.OrderByDescending(d => d.DaysOverdue).ToList(),
                GrandTotal = details.Sum(d => d.PendingAmount)
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // ROOM CHANGE HISTORY
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateRoomChangeHistoryReportAsync(string pgId, DateTime fromDate, DateTime toDate)
        {
            var data = await GetRoomChangeHistoryDataAsync(pgId, fromDate, toDate);
            return new RoomChangeHistoryDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<RoomChangeHistoryReportDto> GetRoomChangeHistoryDataAsync(string pgId, DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.Date;
            var to = toDate.Date;

            var stays = await _db.TenantRooms
                .Include(tr => tr.Tenant)
                .Include(tr => tr.Room)
                .Where(tr => tr.PgId == pgId)
                .OrderBy(tr => tr.TenantId).ThenBy(tr => tr.FromDate)
                .ToListAsync();

            var rows = new List<RoomChangeRowDto>();

            var grouped = stays.GroupBy(s => s.TenantId);
            foreach (var g in grouped)
            {
                var orderedStays = g.OrderBy(s => s.FromDate).ToList();
                for (int i = 1; i < orderedStays.Count; i++)
                {
                    var prev = orderedStays[i - 1];
                    var curr = orderedStays[i];

                    if (!prev.ToDate.HasValue) continue;
                    if (curr.FromDate.Date != prev.ToDate.Value.Date.AddDays(1)) continue;
                    if (prev.RoomId == curr.RoomId) continue; // stay type change, not room change

                    if (curr.FromDate.Date < from || curr.FromDate.Date > to) continue;

                    var oldRent = await _db.RoomRentHistories
                        .Where(r => r.RoomId == prev.RoomId && r.EffectiveFrom <= prev.ToDate
                            && (r.EffectiveTo == null || r.EffectiveTo >= prev.FromDate))
                        .OrderByDescending(r => r.EffectiveFrom)
                        .Select(r => r.RentAmount).FirstOrDefaultAsync();

                    var newRent = await _db.RoomRentHistories
                        .Where(r => r.RoomId == curr.RoomId && r.EffectiveFrom <= curr.FromDate
                            && (r.EffectiveTo == null || r.EffectiveTo >= curr.FromDate))
                        .OrderByDescending(r => r.EffectiveFrom)
                        .Select(r => r.RentAmount).FirstOrDefaultAsync();

                    rows.Add(new RoomChangeRowDto
                    {
                        TenantName = prev.Tenant?.Name ?? "",
                        ChangeDate = curr.FromDate.Date,
                        OldRoom = prev.Room?.RoomNumber ?? "",
                        NewRoom = curr.Room?.RoomNumber ?? "",
                        OldRent = oldRent,
                        NewRent = newRent
                    });
                }
            }

            return new RoomChangeHistoryReportDto
            {
                FromDate = from, ToDate = to,
                Rows = rows.OrderBy(r => r.ChangeDate).ToList(),
                TotalChanges = rows.Count
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // BOOKING CONVERSION
        // ──────────────────────────────────────────────────────────────────────
        public async Task<byte[]> GenerateBookingConversionReportAsync(string pgId, DateTime fromDate, DateTime toDate)
        {
            var data = await GetBookingConversionDataAsync(pgId, fromDate, toDate);
            return new BookingConversionDocument(data, await GetPgProfileAsync(pgId)).GeneratePdf();
        }

        public async Task<BookingConversionReportDto> GetBookingConversionDataAsync(string pgId, DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.Date;
            var to = toDate.Date;

            var bookings = await _db.Bookings
                .Include(b => b.Tenant)
                .Where(b => b.PgId == pgId && b.CreatedAt >= from && b.CreatedAt <= to.AddDays(1))
                .ToListAsync();

            var total = bookings.Count;

            // Checked in = tenant has a TenantRoom with FromDate == ScheduledCheckInDate
            var checkedInIds = new HashSet<string>();
            foreach (var b in bookings.Where(b => b.Status == BookingStatus.Active || b.Status == BookingStatus.Terminated))
            {
                var hasStay = await _db.TenantRooms.AnyAsync(tr =>
                    tr.TenantId == b.TenantId && tr.PgId == pgId && tr.RoomId == b.RoomId);
                if (hasStay) checkedInIds.Add(b.BookingId);
            }

            int checkedIn = checkedInIds.Count;
            int cancelled = bookings.Count(b => b.Status == BookingStatus.Cancelled);
            int expired = bookings.Count(b => b.Status == BookingStatus.Terminated && !checkedInIds.Contains(b.BookingId));
            int stillActive = bookings.Count(b => b.Status == BookingStatus.Active && !checkedInIds.Contains(b.BookingId));

            double convRate = total > 0 ? (double)checkedIn / total * 100 : 0;

            return new BookingConversionReportDto
            {
                FromDate = from, ToDate = to,
                TotalBookings = total,
                CheckedIn = checkedIn,
                Cancelled = cancelled,
                Expired = expired,
                StillActive = stillActive,
                ConversionRatePercent = Math.Round(convRate, 1)
            };
        }

        public async Task<(bool enabled, string? error)> CheckEmailSubscriptionAsync(string pgId)
        {
            var pg = await _db.PGs.FindAsync(pgId);
            if (pg == null) return (false, "PG not found.");
            if (!pg.IsEmailSubscriptionEnabled)
                return (false, "Email subscription is not enabled for this PG. Please purchase an email subscription to use this feature.");
            return (true, null);
        }

        public async Task<(bool enabled, string? error)> CheckWhatsAppSubscriptionAsync(string pgId)
        {
            var pg = await _db.PGs.FindAsync(pgId);
            if (pg == null) return (false, "PG not found.");
            if (!pg.IsWhatsappSubscriptionEnabled)
                return (false, "WhatsApp subscription is not enabled for this PG.");
            return (true, null);
        }

        public async Task<object> GetAvailableRecipientsAsync(string pgId)
        {
            var users = await _db.UserPgs
                .Where(up => up.PgId == pgId)
                .Include(up => up.User)
                .Select(up => new
                {
                    up.UserId,
                    Name = up.User.FullName ?? up.User.UserName ?? "",
                    up.User.Email,
                    up.User.PhoneNumber
                })
                .ToListAsync();
            return users;
        }
    }
}

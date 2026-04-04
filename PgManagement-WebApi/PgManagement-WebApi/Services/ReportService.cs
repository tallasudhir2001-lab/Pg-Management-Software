using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Helpers;
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

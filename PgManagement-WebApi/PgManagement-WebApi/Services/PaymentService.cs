using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Payment;
using PgManagement_WebApi.DTOs.Payment.PendingRent;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Models;
using System.Text.Json;

namespace PgManagement_WebApi.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;
        private readonly IEmailNotificationService _emailService;
        private readonly IWhatsAppNotificationService _whatsAppService;
        private readonly IServiceProvider _serviceProvider;

        public PaymentService(
            ApplicationDbContext context,
            IReportService reportService,
            IEmailNotificationService emailService,
            IWhatsAppNotificationService whatsAppService,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _reportService = reportService;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
            _serviceProvider = serviceProvider;
        }

        public async Task<(bool success, object result, int statusCode)> CreatePaymentAsync(
            CreatePaymentDto dto, string pgId, string? branchId, string userId)
        {
            var tenantExists = await _context.Tenants.AnyAsync(t =>
                t.TenantId == dto.TenantId && t.PgId == pgId && !t.isDeleted);

            if (!tenantExists)
                return (false, "Invalid tenant", 404);

            var stays = await _context.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.TenantId == dto.TenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
                return (false, "Tenant has no stay history", 400);

            var payments = await _context.Payments
                .Where(p =>
                    p.TenantId == dto.TenantId && p.PgId == pgId &&
                    !p.IsDeleted && p.PaymentTypeCode == "RENT")
                .ToListAsync();

            TenantRoom? payableStay = null;
            DateTime payableFrom = DateTime.MinValue;
            DateTime payableUpto = DateTime.MinValue;

            foreach (var stay in stays)
            {
                var stayFrom = stay.FromDate.Date;
                var stayTo = stay.ToDate ?? dto.PaidUpto;
                if (stayFrom > stayTo) continue;

                var unpaidRanges = DateRangeHelper.Subtract(
                    stayFrom, stayTo,
                    payments.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date)));

                if (unpaidRanges.Any())
                {
                    payableStay = stay;
                    payableFrom = unpaidRanges.First().From;
                    payableUpto = stay.ToDate?.Date ?? dto.PaidUpto;
                    break;
                }
            }

            if (payableStay == null)
                return (false, "No pending rent exists for this tenant.", 400);

            if (dto.PaidUpto < payableFrom || dto.PaidUpto > payableUpto)
            {
                return (false,
                    $"Payment period must be between {payableFrom:dd MMM yyyy} and {payableUpto:dd MMM yyyy}. " +
                    $"Please complete payment for Room {payableStay.Room?.RoomNumber ?? payableStay.RoomId} first.",
                    400);
            }

            var payment = new Payment
            {
                PaymentId = Guid.NewGuid().ToString(),
                PgId = pgId,
                BranchId = branchId,
                TenantId = dto.TenantId,
                Amount = dto.Amount,
                PaymentDate = dto.PaymentDate ?? DateTime.UtcNow,
                PaidFrom = payableFrom,
                PaidUpto = dto.PaidUpto,
                PaymentFrequencyCode = dto.PaymentFrequencyCode,
                PaymentModeCode = dto.PaymentModeCode,
                PaymentTypeCode = "RENT",
                Notes = dto.Notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Auto-send receipt in background
            var paymentId = payment.PaymentId;
            var tenantId = dto.TenantId;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

                    var notifSettings = await db.NotificationSettings
                        .FirstOrDefaultAsync(ns => ns.PgId == pgId);

                    if (notifSettings?.AutoSendPaymentReceipt == true)
                    {
                        var pg = await db.PGs.FindAsync(pgId);
                        var tenant = await db.Tenants.FindAsync(tenantId);
                        var tenantEmail = tenant?.Email;
                        var tenantPhone = tenant?.ContactNumber;

                        if (notifSettings.SendViaEmail && pg?.IsEmailSubscriptionEnabled == true
                            && !string.IsNullOrWhiteSpace(tenantEmail))
                        {
                            await emailSvc.SendPaymentReceiptAsync(paymentId, pgId, tenantEmail);
                        }

                        if (notifSettings.SendViaWhatsapp && pg?.IsWhatsappSubscriptionEnabled == true
                            && !string.IsNullOrWhiteSpace(tenantPhone))
                        {
                            var whatsAppSvc = scope.ServiceProvider.GetRequiredService<IWhatsAppNotificationService>();
                            await whatsAppSvc.SendPaymentReceiptAsync(paymentId, pgId, tenantPhone);
                        }
                    }
                }
                catch { }
            });

            return (true, new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                PaidFrom = payment.PaidFrom,
                PaidUpto = payment.PaidUpto,
                Amount = payment.Amount
            }, 200);
        }

        public async Task<PendingRentResponseDto> GetPendingRentAsync(
            string tenantId, string pgId, DateTime? asOfDate)
        {
            var asOf = (asOfDate ?? DateTime.Now).Date;

            var tenantExists = await _context.Tenants.AnyAsync(t =>
                t.TenantId == tenantId && t.PgId == pgId && !t.isDeleted);

            if (!tenantExists)
                return new PendingRentResponseDto
                {
                    TenantId = tenantId, AsOfDate = asOf,
                    TotalPendingAmount = 0, Breakdown = new List<PendingRentBreakdownDto>()
                };

            var stays = await _context.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
                return new PendingRentResponseDto
                {
                    TenantId = tenantId, AsOfDate = asOf,
                    TotalPendingAmount = 0, Breakdown = new List<PendingRentBreakdownDto>()
                };

            var payments = await _context.Payments
                .Where(p => p.TenantId == tenantId && p.PgId == pgId && !p.IsDeleted && p.PaymentTypeCode == "RENT")
                .ToListAsync();

            decimal totalPending = 0;
            var breakdown = new List<PendingRentBreakdownDto>();

            foreach (var stay in stays)
            {
                var stayFrom = stay.FromDate.Date;
                var stayTo = stay.ToDate.HasValue ? Min(stay.ToDate.Value, asOf) : asOf.Date;
                if (stayFrom > stayTo) continue;

                var unpaidRanges = DateRangeHelper.Subtract(stayFrom, stayTo,
                    payments.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date)));
                if (!unpaidRanges.Any()) continue;

                var rentHistories = await _context.RoomRentHistories
                    .Where(r => r.RoomId == stay.RoomId && r.EffectiveFrom <= stayTo
                        && (r.EffectiveTo == null || r.EffectiveTo >= stayFrom))
                    .OrderBy(r => r.EffectiveFrom)
                    .ToListAsync();

                foreach (var range in unpaidRanges)
                {
                    var rentSlices = RentHelper.GetRentSlices(
                        range.From.Date, range.To.Date, rentHistories, stay.StayType,
                        stayFromDate: stay.FromDate.Date, isActiveStay: stay.ToDate == null);

                    foreach (var slice in rentSlices)
                    {
                        totalPending += slice.Amount;
                        breakdown.Add(new PendingRentBreakdownDto
                        {
                            FromDate = slice.From, ToDate = slice.To,
                            RentPerDay = slice.RentPerDay, Amount = slice.Amount,
                            RoomNumber = stay.Room.RoomNumber
                        });
                    }
                }
            }

            return new PendingRentResponseDto
            {
                TenantId = tenantId, AsOfDate = asOf,
                TotalPendingAmount = Decimal.Round(totalPending, 2, MidpointRounding.AwayFromZero),
                Breakdown = breakdown
            };
        }

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        public async Task<(bool success, object result, int statusCode)> CalculateRentAsync(
            string tenantId, string pgId, DateTime paidFrom, DateTime paidUpto)
        {
            if (paidFrom > paidUpto)
                return (false, "paidFrom must be <= paidUpto", 400);

            var stay = await _context.TenantRooms
                .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId
                    && tr.FromDate <= paidFrom
                    && (tr.ToDate == null || tr.ToDate >= paidFrom))
                .OrderByDescending(tr => tr.FromDate)
                .FirstOrDefaultAsync();

            if (stay == null)
            {
                stay = await _context.TenantRooms
                    .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId
                        && tr.FromDate <= paidUpto
                        && (tr.ToDate == null || tr.ToDate >= paidFrom))
                    .OrderByDescending(tr => tr.FromDate)
                    .FirstOrDefaultAsync();
            }

            if (stay == null)
                return (false, "No stay found for the given period", 404);

            var rentHistories = await _context.RoomRentHistories
                .Where(r => r.RoomId == stay.RoomId
                    && r.EffectiveFrom <= paidUpto
                    && (r.EffectiveTo == null || r.EffectiveTo >= paidFrom))
                .OrderBy(r => r.EffectiveFrom)
                .ToListAsync();

            var slices = RentHelper.GetRentSlices(paidFrom, paidUpto, rentHistories, stay.StayType,
                stayFromDate: stay.FromDate.Date, isActiveStay: false);
            var amount = slices.Sum(s => s.Amount);

            return (true, new { amount = Decimal.Round(amount, 2), stayType = stay.StayType }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> GetPaymentContextAsync(
            string tenantId, string pgId)
        {
            var today = DateTime.Now.Date;

            var tenant = await _context.Tenants
                .Where(t => t.TenantId == tenantId && t.PgId == pgId && !t.isDeleted)
                .Select(t => new { t.TenantId, t.Name })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return (false, "Invalid tenant", 404);

            var stays = await _context.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
                return (false, "Tenant has no stay history", 400);

            var payments = await _context.Payments
                .Where(p => p.TenantId == tenantId && p.PgId == pgId && !p.IsDeleted && p.PaymentTypeCode == "RENT")
                .ToListAsync();

            var pendingStays = new List<PendingStayContextDto>();

            foreach (var stay in stays)
            {
                var stayFrom = stay.FromDate.Date;
                DateTime stayTo;

                if (stay.ToDate.HasValue)
                {
                    stayTo = Min(stay.ToDate.Value, today);
                }
                else if ((stay.StayType ?? "MONTHLY") == "MONTHLY")
                {
                    var cycleDay = stay.FromDate.Day;
                    stayTo = RentHelper.GetCycleEnd(today, cycleDay);
                }
                else
                {
                    stayTo = today;
                }

                if (stayFrom > stayTo) continue;

                var unpaidRanges = DateRangeHelper.Subtract(stayFrom, stayTo,
                    payments.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date)));
                if (!unpaidRanges.Any()) continue;

                var rentHistories = await _context.RoomRentHistories
                    .Where(r => r.RoomId == stay.RoomId && r.EffectiveFrom <= stayTo
                        && (r.EffectiveTo == null || r.EffectiveTo >= stayFrom))
                    .OrderBy(r => r.EffectiveFrom)
                    .ToListAsync();

                decimal stayPending = 0;
                foreach (var range in unpaidRanges)
                {
                    var slices = RentHelper.GetRentSlices(range.From, range.To, rentHistories, stay.StayType,
                        stayFromDate: stay.FromDate.Date, isActiveStay: stay.ToDate == null);
                    stayPending += slices.Sum(s => s.Amount);
                }

                var currentRentHistory = rentHistories
                    .Where(r => r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today))
                    .OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();

                if (stayPending > 0)
                {
                    pendingStays.Add(new PendingStayContextDto
                    {
                        RoomId = stay.RoomId,
                        RoomNumber = stay.Room.RoomNumber,
                        FromDate = unpaidRanges.First().From,
                        ToDate = unpaidRanges.Last().To,
                        StayStartDate = stay.FromDate.Date,
                        PendingAmount = Decimal.Round(stayPending, 2),
                        IsActiveStay = stay.ToDate == null,
                        IsNextPayable = false,
                        RentPerMonth = currentRentHistory?.RentAmount ?? 0,
                        StayType = stay.StayType ?? "MONTHLY"
                    });
                }
            }

            if (!pendingStays.Any())
            {
                var activeStayForRent = stays.FirstOrDefault(s => s.ToDate == null);
                decimal activeRentPerMonth = 0;
                string? activeRoomNumber = activeStayForRent?.Room?.RoomNumber;

                if (activeStayForRent != null)
                {
                    var activeRentHistory = await _context.RoomRentHistories
                        .Where(r => r.RoomId == activeStayForRent.RoomId &&
                                    r.EffectiveFrom <= today &&
                                    (r.EffectiveTo == null || r.EffectiveTo >= today))
                        .OrderByDescending(r => r.EffectiveFrom)
                        .FirstOrDefaultAsync();
                    activeRentPerMonth = activeRentHistory?.RentAmount ?? 0;
                }

                return (true, new PaymentContextDto
                {
                    TenantId = tenant.TenantId, TenantName = tenant.Name,
                    PendingAmount = 0, PendingStays = new List<PendingStayContextDto>(),
                    PaidFrom = null, MaxPaidUpto = null,
                    HasActiveStay = false, AsOfDate = today,
                    RoomNumber = activeRoomNumber, RentPerMonth = activeRentPerMonth,
                    StayType = stays.LastOrDefault()?.StayType ?? "MONTHLY"
                }, 200);
            }

            var firstPayableStay = pendingStays.First();
            firstPayableStay.IsNextPayable = true;

            return (true, new PaymentContextDto
            {
                TenantId = tenant.TenantId, TenantName = tenant.Name,
                PendingAmount = pendingStays.Sum(s => s.PendingAmount),
                PendingStays = pendingStays,
                PaidFrom = firstPayableStay.FromDate,
                MaxPaidUpto = firstPayableStay.ToDate,
                HasActiveStay = pendingStays.Any(s => s.IsActiveStay),
                AsOfDate = today,
                RoomNumber = firstPayableStay.RoomNumber,
                RentPerMonth = firstPayableStay.RentPerMonth,
                StayType = firstPayableStay.StayType,
                StayStartDate = firstPayableStay.StayStartDate
            }, 200);
        }

        public async Task<object> GetPaymentHistoryForTenantAsync(string tenantId, string pgId)
        {
            return await _context.Payments
                .Include(p => p.PaymentType)
                .Include(p => p.CreatedByUser)
                .Where(p => p.TenantId == tenantId && p.PgId == pgId && !p.IsDeleted)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new TenantPaymentHistoryDto
                {
                    PaymentId = p.PaymentId,
                    PaymentType = p.PaymentType.Name,
                    PaymentDate = p.PaymentDate,
                    PaidFrom = p.PaidFrom,
                    PaidUpto = p.PaidUpto,
                    Amount = p.Amount,
                    PaymentMode = p.PaymentModeCode,
                    Frequency = p.PaymentFrequencyCode.ToString(),
                    CollectedBy = p.CreatedByUser.FullName ?? p.CreatedByUser.UserName
                })
                .ToListAsync();
        }

        public async Task<PageResultsDto<PaymentHistoryDto>> GetPaymentHistoryAsync(
            List<string> pgIds, int page, int pageSize, string? search, string? mode,
            string? tenantId, string? userId, string? types, string sortBy, string sortDir)
        {
            var query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Tenant)
                .Include(p => p.CreatedByUser)
                .Include(p => p.PaymentType)
                .Where(p => pgIds.Contains(p.PgId) && !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.PaymentId.ToLower().Contains(term) ||
                    p.Tenant.ContactNumber.Contains(term) ||
                    p.Tenant.AadharNumber.Contains(term));
            }

            if (!string.IsNullOrEmpty(mode))
            {
                var modeCodes = mode
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(m => m.ToLower()).ToList();
                if (modeCodes.Any())
                    query = query.Where(p => modeCodes.Contains(p.PaymentModeCode.ToLower()));
            }

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(p => p.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(p => p.CreatedByUserId == userId);

            if (!string.IsNullOrEmpty(types))
            {
                var typeCodes = types
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToUpper()).ToList();
                if (typeCodes.Any())
                    query = query.Where(p => typeCodes.Contains(p.PaymentTypeCode));
            }

            var totalCount = await query.CountAsync();

            query = sortBy.ToLower() switch
            {
                "tenantname" => sortDir == "asc" ? query.OrderBy(p => p.Tenant.Name) : query.OrderByDescending(p => p.Tenant.Name),
                "amount" => sortDir == "asc" ? query.OrderBy(p => p.Amount) : query.OrderByDescending(p => p.Amount),
                "mode" => sortDir == "asc" ? query.OrderBy(p => p.PaymentModeCode) : query.OrderByDescending(p => p.PaymentModeCode),
                "periodcovered" => sortDir == "asc" ? query.OrderBy(p => p.PaidFrom) : query.OrderByDescending(p => p.PaidFrom),
                "paymenttype" => sortDir == "asc" ? query.OrderBy(p => p.PaymentType.Name) : query.OrderByDescending(p => p.PaymentType.Name),
                _ => sortDir == "asc" ? query.OrderBy(p => p.PaymentDate) : query.OrderByDescending(p => p.PaymentDate)
            };

            var payments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentHistoryDto
                {
                    PaymentId = p.PaymentId,
                    PaymentType = p.PaymentType.Name,
                    PaymentDate = p.PaymentDate,
                    TenantName = p.Tenant.Name,
                    PeriodCovered = p.PaidFrom.ToString("dd MMM yyyy") + " - " + p.PaidUpto.ToString("dd MMM yyyy"),
                    Amount = p.Amount,
                    Mode = p.PaymentModeCode,
                    CollectedBy = p.CreatedByUser.FullName ?? p.CreatedByUser.UserName!
                })
                .ToListAsync();

            return new PageResultsDto<PaymentHistoryDto> { Items = payments, TotalCount = totalCount };
        }

        public async Task<(bool success, object result, int statusCode)> DeletePaymentAsync(
            string paymentId, string pgId, string userId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.PgId == pgId && !p.IsDeleted);

            if (payment == null)
            {
                var otherPgName = await _context.Payments
                    .Where(p => p.PaymentId == paymentId && !p.IsDeleted)
                    .Join(_context.PGs, p => p.PgId, pg => pg.PgId, (p, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return (false, $"This payment belongs to {otherPgName}. Please login to {otherPgName} to modify it.", 403);
                return (false, "Payment not found", 404);
            }

            if (payment.PaymentTypeCode == "RENT")
            {
                var hasLaterRentPayments = await _context.Payments.AnyAsync(p =>
                    p.TenantId == payment.TenantId && p.PgId == pgId &&
                    !p.IsDeleted && p.PaymentTypeCode == "RENT" && p.PaidFrom > payment.PaidFrom);

                if (hasLaterRentPayments)
                    return (false, "This payment cannot be deleted because newer rent payments exist.", 400);
            }
            else if (payment.PaymentTypeCode == "ADVANCE_PAYMENT")
            {
                var advance = await _context.Advances
                    .FirstOrDefaultAsync(a =>
                        a.TenantId == payment.TenantId && !a.IsSettled && a.Amount == payment.Amount);

                if (advance == null)
                {
                    var settledAdvance = await _context.Advances
                        .AnyAsync(a => a.TenantId == payment.TenantId && a.IsSettled && a.Amount == payment.Amount);
                    if (settledAdvance)
                        return (false, "This advance payment cannot be deleted because the advance has already been settled. Delete the settlement first.", 400);
                }
                else
                {
                    _context.Advances.Remove(advance);
                }
            }
            else if (payment.PaymentTypeCode == "ADVANCE_REFUND")
            {
                var advance = await _context.Advances
                    .FirstOrDefaultAsync(a =>
                        a.TenantId == payment.TenantId && a.IsSettled && a.SettledDate != null);

                if (advance != null)
                {
                    advance.IsSettled = false;
                    advance.DeductedAmount = 0;
                    advance.SettledDate = null;
                    advance.SettledByUserId = null;
                    advance.Notes = null;
                }
            }

            payment.IsDeleted = true;
            payment.DeletedAt = DateTime.UtcNow;
            payment.DeletedByUserId = userId;

            _context.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                PgId = pgId,
                BranchId = payment.BranchId,
                EventType = "PAYMENT_DELETED",
                EntityType = "Payment",
                EntityId = paymentId,
                Description = $"{payment.PaymentTypeCode} payment of ₹{payment.Amount} deleted (Tenant: {payment.TenantId})",
                OldValue = JsonSerializer.Serialize(new
                {
                    payment.Amount, payment.PaidFrom, payment.PaidUpto,
                    payment.PaymentModeCode, payment.PaymentTypeCode, payment.TenantId
                }),
                PerformedByUserId = userId,
                PerformedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return (true, "OK", 200);
        }

        public async Task<object?> GetPaymentAsync(string paymentId, List<string> pgIds)
        {
            return await _context.Payments
                .Where(p => p.PaymentId == paymentId && pgIds.Contains(p.PgId) && !p.IsDeleted)
                .Select(p => new
                {
                    p.PaymentId, p.TenantId, p.PaymentDate,
                    p.PaidFrom, p.PaidUpto, p.Amount,
                    p.PaymentModeCode, p.PaymentFrequencyCode, p.Notes
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool success, object result, int statusCode)> UpdatePaymentAsync(
            string paymentId, string pgId, UpdatePaymentDto dto, string userId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.PgId == pgId && !p.IsDeleted);

            if (payment == null)
            {
                var otherPgName = await _context.Payments
                    .Where(p => p.PaymentId == paymentId && !p.IsDeleted)
                    .Join(_context.PGs, p => p.PgId, pg => pg.PgId, (p, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return (false, $"This payment belongs to {otherPgName}. Please login to {otherPgName} to modify it.", 403);
                return (false, "Payment not found", 404);
            }

            var hasLaterPayments = await _context.Payments.AnyAsync(p =>
                p.TenantId == payment.TenantId && p.PgId == pgId &&
                !p.IsDeleted && p.PaidFrom > payment.PaidFrom);

            if (hasLaterPayments)
            {
                if (dto.PaidFrom != payment.PaidFrom || dto.PaidUpto != payment.PaidUpto)
                    return (false, "Changes not saved\nYou can only modify dates for latest payment. Page Refreshed", 400);
            }

            var oldAmount = payment.Amount;
            var oldPaidFrom = payment.PaidFrom;
            var oldPaidUpto = payment.PaidUpto;

            payment.Amount = dto.Amount;
            payment.PaymentModeCode = dto.PaymentModeCode;
            payment.PaymentFrequencyCode = dto.PaymentFrequencyCode;
            payment.Notes = dto.Notes;

            if (!hasLaterPayments)
            {
                payment.PaidFrom = dto.PaidFrom;
                payment.PaidUpto = dto.PaidUpto;
            }

            if (oldAmount != dto.Amount)
            {
                _context.AuditEvents.Add(new AuditEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    PgId = pgId, BranchId = payment.BranchId,
                    EventType = "PAYMENT_AMOUNT_CHANGED", EntityType = "Payment", EntityId = paymentId,
                    Description = $"Payment amount changed from ₹{oldAmount} to ₹{dto.Amount}",
                    OldValue = JsonSerializer.Serialize(new { Amount = oldAmount }),
                    NewValue = JsonSerializer.Serialize(new { Amount = dto.Amount }),
                    PerformedByUserId = userId, PerformedAt = DateTime.UtcNow
                });
            }

            if (oldPaidFrom != payment.PaidFrom || oldPaidUpto != payment.PaidUpto)
            {
                _context.AuditEvents.Add(new AuditEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    PgId = pgId, BranchId = payment.BranchId,
                    EventType = "PAYMENT_PERIOD_CHANGED", EntityType = "Payment", EntityId = paymentId,
                    Description = $"Payment period changed from {oldPaidFrom:dd MMM yyyy}–{oldPaidUpto:dd MMM yyyy} to {payment.PaidFrom:dd MMM yyyy}–{payment.PaidUpto:dd MMM yyyy}",
                    OldValue = JsonSerializer.Serialize(new { PaidFrom = oldPaidFrom, PaidUpto = oldPaidUpto }),
                    NewValue = JsonSerializer.Serialize(new { payment.PaidFrom, payment.PaidUpto }),
                    PerformedByUserId = userId, PerformedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return (true, "OK", 200);
        }

        public async Task<(bool success, object result, int statusCode)> SendReceiptAsync(
            string paymentId, string pgId)
        {
            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null) return (false, "PG not found.", 404);
            if (!pg.IsEmailSubscriptionEnabled)
                return (false, "Email subscription is not enabled for this PG. Please purchase an email subscription to use this feature.", 400);

            var payment = await _context.Payments
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.PgId == pgId && !p.IsDeleted);
            if (payment == null) return (false, "Payment not found.", 404);

            var email = payment.Tenant?.Email;
            if (string.IsNullOrWhiteSpace(email))
                return (false, "Tenant does not have an email address on file.", 422);

            await _emailService.SendPaymentReceiptAsync(paymentId, pgId, email);
            return (true, new { message = $"Receipt sent to {email}" }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> SendReceiptWhatsAppAsync(
            string paymentId, string pgId)
        {
            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null) return (false, "PG not found.", 404);
            if (!pg.IsWhatsappSubscriptionEnabled)
                return (false, "WhatsApp subscription is not enabled for this PG. Please purchase a WhatsApp subscription to use this feature.", 400);

            var payment = await _context.Payments
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.PgId == pgId && !p.IsDeleted);
            if (payment == null) return (false, "Payment not found.", 404);

            var phone = payment.Tenant?.ContactNumber;
            if (string.IsNullOrWhiteSpace(phone))
                return (false, "Tenant does not have a phone number on file.", 422);

            await _whatsAppService.SendPaymentReceiptAsync(paymentId, pgId, phone);
            return (true, new { message = $"Receipt sent via WhatsApp to {phone}" }, 200);
        }
    }
}

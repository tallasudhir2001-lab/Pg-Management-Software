using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Payment;
using PgManagement_WebApi.DTOs.Payment.PendingRent;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Migrations;
using PgManagement_WebApi.Models;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.DTOs.Payment
{
    public class SendReceiptDto
    {
        public string RecipientEmail { get; set; } = "";
    }
}

namespace PgManagement_WebApi.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        private readonly IReportService reportService;
        private readonly IEmailNotificationService emailService;
        private readonly IWhatsAppNotificationService whatsAppService;

        public PaymentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IReportService reportService,
            IEmailNotificationService emailService,
            IWhatsAppNotificationService whatsAppService)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
            this.reportService = reportService;
            this.emailService = emailService;
            this.whatsAppService = whatsAppService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET {paymentId}/receipt  — returns PDF receipt
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{paymentId}/receipt")]
        public async Task<IActionResult> GetReceipt(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var pdf = await reportService.GenerateReceiptAsync(paymentId, pgId);
            return File(pdf, "application/pdf", $"Receipt_{paymentId[^6..].ToUpper()}.pdf");
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST {paymentId}/send-receipt  — sends receipt to tenant's email on file
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("{paymentId}/send-receipt")]
        public async Task<IActionResult> SendReceipt(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var pg = await context.PGs.FindAsync(pgId);
            if (pg == null) return NotFound("PG not found.");

            if (!pg.IsEmailSubscriptionEnabled)
                return BadRequest("Email subscription is not enabled for this PG. Please purchase an email subscription to use this feature.");

            var payment = await context.Payments
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.PgId == pgId && !p.IsDeleted);

            if (payment == null)
                return NotFound("Payment not found.");

            var email = payment.Tenant?.Email;
            if (string.IsNullOrWhiteSpace(email))
                return UnprocessableEntity("Tenant does not have an email address on file.");

            await emailService.SendPaymentReceiptAsync(paymentId, pgId, email);
            return Ok(new { message = $"Receipt sent to {email}" });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST {paymentId}/send-receipt-whatsapp  — sends receipt to tenant's WhatsApp
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("{paymentId}/send-receipt-whatsapp")]
        public async Task<IActionResult> SendReceiptWhatsApp(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var pg = await context.PGs.FindAsync(pgId);
            if (pg == null) return NotFound("PG not found.");

            if (!pg.IsWhatsappSubscriptionEnabled)
                return BadRequest("WhatsApp subscription is not enabled for this PG. Please purchase a WhatsApp subscription to use this feature.");

            var payment = await context.Payments
                .Include(p => p.Tenant)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.PgId == pgId && !p.IsDeleted);

            if (payment == null)
                return NotFound("Payment not found.");

            var phone = payment.Tenant?.ContactNumber;
            if (string.IsNullOrWhiteSpace(phone))
                return UnprocessableEntity("Tenant does not have a phone number on file.");

            await whatsAppService.SendPaymentReceiptAsync(paymentId, pgId, phone);
            return Ok(new { message = $"Receipt sent via WhatsApp to {phone}" });
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST create-payment  — ORIGINAL, fully intact
        // ─────────────────────────────────────────────────────────────────────
        [AccessPoint("Payment", "Create Payment")]
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment(CreatePaymentDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId) || string.IsNullOrEmpty(userId))
                return Unauthorized();

            var today = DateTime.Now.Date;

            // 1️⃣ Validate tenant
            var tenantExists = await context.Tenants.AnyAsync(t =>
                t.TenantId == dto.TenantId &&
                t.PgId == pgId &&
                !t.isDeleted);

            if (!tenantExists)
                return NotFound("Invalid tenant");

            // 2️⃣ Load stays (chronological)
            var stays = await context.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.TenantId == dto.TenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
                return BadRequest("Tenant has no stay history");

            // 3️⃣ Load existing payments
            var payments = await context.Payments
                .Where(p =>
                    p.TenantId == dto.TenantId &&
                    p.PgId == pgId &&
                    !p.IsDeleted && p.PaymentTypeCode == "RENT")
                .ToListAsync();

            // 4️⃣ Find the FIRST stay with unpaid ranges (pay stays in order)
            TenantRoom? payableStay = null;
            DateTime payableFrom = DateTime.MinValue;
            DateTime payableUpto = DateTime.MinValue;

            foreach (var stay in stays)
            {
                var stayFrom = stay.FromDate.Date;
                var stayTo = stay.ToDate ?? dto.PaidUpto;

                if (stayFrom > stayTo)
                    continue;

                var unpaidRanges = DateRangeHelper.Subtract(
                    stayFrom,
                    stayTo,
                    payments.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date))
                );

                if (unpaidRanges.Any())
                {
                    payableStay = stay;
                    payableFrom = unpaidRanges.First().From;
                    payableUpto = stay.ToDate?.Date ?? dto.PaidUpto;
                    break; // first unpaid stay wins
                }
            }

            if (payableStay == null)
                return BadRequest("No pending rent exists for this tenant.");

            // 5️⃣ Validate requested payment period — must be within the first pending stay
            if (dto.PaidUpto < payableFrom || dto.PaidUpto > payableUpto)
            {
                return BadRequest(
                    $"Payment period must be between {payableFrom:dd MMM yyyy} and {payableUpto:dd MMM yyyy}. " +
                    $"Please complete payment for Room {payableStay.Room?.RoomNumber ?? payableStay.RoomId} first."
                );
            }

            // 6️⃣ Create payment (PaidFrom is enforced by backend)
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

            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            // Auto-send payment receipt in background if configured
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

                    var notifSettings = await db.NotificationSettings
                        .FirstOrDefaultAsync(ns => ns.PgId == pgId);

                    if (notifSettings?.AutoSendPaymentReceipt == true)
                    {
                        var pg = await db.PGs.FindAsync(pgId);
                        var tenant = await db.Tenants.FindAsync(dto.TenantId);
                        var tenantEmail = tenant?.Email;
                        var tenantPhone = tenant?.ContactNumber;

                        if (notifSettings.SendViaEmail && pg?.IsEmailSubscriptionEnabled == true
                            && !string.IsNullOrWhiteSpace(tenantEmail))
                        {
                            await emailSvc.SendPaymentReceiptAsync(payment.PaymentId, pgId, tenantEmail);
                        }

                        if (notifSettings.SendViaWhatsapp && pg?.IsWhatsappSubscriptionEnabled == true
                            && !string.IsNullOrWhiteSpace(tenantPhone))
                        {
                            var whatsAppSvc = scope.ServiceProvider.GetRequiredService<IWhatsAppNotificationService>();
                            await whatsAppSvc.SendPaymentReceiptAsync(payment.PaymentId, pgId, tenantPhone);
                        }
                    }
                }
                catch
                {
                    // Background send failed — don't affect the user
                }
            });

            return Ok(new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                PaidFrom = payment.PaidFrom,
                PaidUpto = payment.PaidUpto,
                Amount = payment.Amount
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET pending/{tenantId}  — ORIGINAL, fully intact
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("pending/{tenantId}")]
        public async Task<IActionResult> GetPendingRent(
            string tenantId,
            DateTime? asOfDate = null)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var asOf = (asOfDate ?? DateTime.Now).Date;

            // 1️ Validate tenant
            var tenantExists = await context.Tenants.AnyAsync(t =>
                t.TenantId == tenantId &&
                t.PgId == pgId &&
                !t.isDeleted);

            if (!tenantExists)
                return NotFound("Invalid tenant");

            // 2️ Load all stays
            var stays = await context.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
            {
                return Ok(new PendingRentResponseDto
                {
                    TenantId = tenantId,
                    AsOfDate = asOf,
                    TotalPendingAmount = 0,
                    Breakdown = new List<PendingRentBreakdownDto>()
                });
            }

            // 3️ Load all payments
            var payments = await context.Payments
                .Where(p => p.TenantId == tenantId && p.PgId == pgId && !p.IsDeleted && p.PaymentTypeCode == "RENT")
                .ToListAsync();

            decimal totalPending = 0;
            var breakdown = new List<PendingRentBreakdownDto>();

            // 4️ Iterate each stay
            foreach (var stay in stays)
            {
                var stayFrom = stay.FromDate.Date;
                var stayTo = stay.ToDate.HasValue
                    ? Min(stay.ToDate.Value, asOf)
                    : asOf.Date;

                if (stayFrom > stayTo)
                    continue;

                // 5️ Subtract paid ranges
                var unpaidRanges = DateRangeHelper.Subtract(
                    stayFrom,
                    stayTo,
                    payments.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date))
                );

                if (!unpaidRanges.Any())
                    continue;

                // 6️ Load rent history ONCE per stay
                var rentHistories = await context.RoomRentHistories
                    .Where(r =>
                        r.RoomId == stay.RoomId &&
                        r.EffectiveFrom <= stayTo &&
                        (r.EffectiveTo == null || r.EffectiveTo >= stayFrom))
                    .OrderBy(r => r.EffectiveFrom)
                    .ToListAsync();

                // 7️ Calculate rent for each unpaid range
                foreach (var range in unpaidRanges)
                {
                    var rentSlices = RentHelper.GetRentSlices(
                        range.From.Date,
                        range.To.Date,
                        rentHistories,
                        stay.StayType,
                        stayFromDate: stay.FromDate.Date,
                        isActiveStay: stay.ToDate == null
                    );

                    foreach (var slice in rentSlices)
                    {
                        totalPending += slice.Amount;

                        breakdown.Add(new PendingRentBreakdownDto
                        {
                            FromDate = slice.From,
                            ToDate = slice.To,
                            RentPerDay = slice.RentPerDay,
                            Amount = slice.Amount,
                            RoomNumber = stay.Room.RoomNumber
                        });
                    }
                }
            }

            return Ok(new PendingRentResponseDto
            {
                TenantId = tenantId,
                AsOfDate = asOf,
                TotalPendingAmount = Decimal.Round(
                    totalPending,
                    2,
                    MidpointRounding.AwayFromZero),
                Breakdown = breakdown
            });
        }

        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        // ─────────────────────────────────────────────────────────────────────
        // GET calculate-rent — returns rent amount for a given paidFrom→paidUpto
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("calculate-rent/{tenantId}")]
        public async Task<IActionResult> CalculateRent(
            string tenantId,
            [FromQuery] DateTime paidFrom,
            [FromQuery] DateTime paidUpto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            if (paidFrom > paidUpto)
                return BadRequest("paidFrom must be <= paidUpto");

            // Find the stay that covers paidFrom
            var stay = await context.TenantRooms
                .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId
                    && tr.FromDate <= paidFrom
                    && (tr.ToDate == null || tr.ToDate >= paidFrom))
                .OrderByDescending(tr => tr.FromDate)
                .FirstOrDefaultAsync();

            if (stay == null)
            {
                // Try any stay that overlaps the range
                stay = await context.TenantRooms
                    .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId
                        && tr.FromDate <= paidUpto
                        && (tr.ToDate == null || tr.ToDate >= paidFrom))
                    .OrderByDescending(tr => tr.FromDate)
                    .FirstOrDefaultAsync();
            }

            if (stay == null)
                return NotFound("No stay found for the given period");

            var rentHistories = await context.RoomRentHistories
                .Where(r => r.RoomId == stay.RoomId
                    && r.EffectiveFrom <= paidUpto
                    && (r.EffectiveTo == null || r.EffectiveTo >= paidFrom))
                .OrderBy(r => r.EffectiveFrom)
                .ToListAsync();

            // isActiveStay: false — the user explicitly chose paidUpto,
            // so don't extend the period to the billing cycle end.
            var slices = RentHelper.GetRentSlices(paidFrom, paidUpto, rentHistories, stay.StayType,
                stayFromDate: stay.FromDate.Date, isActiveStay: false);
            var amount = slices.Sum(s => s.Amount);

            return Ok(new { amount = Decimal.Round(amount, 2), stayType = stay.StayType });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET context/{tenantId}  — ORIGINAL, fully intact
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("context/{tenantId}")]
        public async Task<IActionResult> GetPaymentContext(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var today = DateTime.Now.Date;

            // 1️⃣ Validate tenant
            var tenant = await context.Tenants
                .Where(t => t.TenantId == tenantId && t.PgId == pgId && !t.isDeleted)
                .Select(t => new { t.TenantId, t.Name })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound("Invalid tenant");

            // 2️⃣ Load stays (chronological)
            var stays = await context.TenantRooms
                .Include(tr => tr.Room)
                .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
                return BadRequest("Tenant has no stay history");

            // 3️⃣ Load all payments once
            var payments = await context.Payments
                .Where(p => p.TenantId == tenantId && p.PgId == pgId && !p.IsDeleted && p.PaymentTypeCode == "RENT")
                .ToListAsync();

            var pendingStays = new List<PendingStayContextDto>();

            // 4️⃣ Compute unpaid ranges per stay
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
                    // For MONTHLY active stays, extend to billing cycle end
                    // so the full current cycle is payable
                    var cycleDay = stay.FromDate.Day;
                    stayTo = RentHelper.GetCycleEnd(today, cycleDay);
                }
                else
                {
                    stayTo = today;
                }

                if (stayFrom > stayTo)
                    continue;

                var unpaidRanges = DateRangeHelper.Subtract(
                    stayFrom,
                    stayTo,
                    payments.Select(p => (p.PaidFrom.Date, p.PaidUpto.Date))
                );

                if (!unpaidRanges.Any())
                    continue;

                var rentHistories = await context.RoomRentHistories
                    .Where(r =>
                        r.RoomId == stay.RoomId &&
                        r.EffectiveFrom <= stayTo &&
                        (r.EffectiveTo == null || r.EffectiveTo >= stayFrom))
                    .OrderBy(r => r.EffectiveFrom)
                    .ToListAsync();

                decimal stayPending = 0;

                foreach (var range in unpaidRanges)
                {
                    var slices = RentHelper.GetRentSlices(
                        range.From,
                        range.To,
                        rentHistories,
                        stay.StayType,
                        stayFromDate: stay.FromDate.Date,
                        isActiveStay: stay.ToDate == null
                    );

                    stayPending += slices.Sum(s => s.Amount);
                }

                var currentRentHistory = rentHistories
                    .Where(r => r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo >= today))
                    .OrderByDescending(r => r.EffectiveFrom)
                    .FirstOrDefault();

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
                        IsNextPayable = false, // set later
                        RentPerMonth = currentRentHistory?.RentAmount ?? 0,
                        StayType = stay.StayType ?? "MONTHLY"
                    });
                }
            }

            // 5️⃣ No pending rent at all
            if (!pendingStays.Any())
            {
                var activeStayForRent = stays.FirstOrDefault(s => s.ToDate == null);
                decimal activeRentPerMonth = 0;
                string? activeRoomNumber = activeStayForRent?.Room?.RoomNumber;

                if (activeStayForRent != null)
                {
                    var activeRentHistory = await context.RoomRentHistories
                        .Where(r => r.RoomId == activeStayForRent.RoomId &&
                                    r.EffectiveFrom <= today &&
                                    (r.EffectiveTo == null || r.EffectiveTo >= today))
                        .OrderByDescending(r => r.EffectiveFrom)
                        .FirstOrDefaultAsync();

                    activeRentPerMonth = activeRentHistory?.RentAmount ?? 0;
                }

                return Ok(new PaymentContextDto
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    PendingAmount = 0,
                    PendingStays = new List<PendingStayContextDto>(),
                    PaidFrom = null,
                    MaxPaidUpto = null,
                    HasActiveStay = false,
                    AsOfDate = today,
                    RoomNumber = activeRoomNumber,
                    RentPerMonth = activeRentPerMonth,
                    StayType = stays.LastOrDefault()?.StayType ?? "MONTHLY"
                });
            }

            // 6️⃣ Payment window = FIRST pending stay only
            //    Different stays may have different rooms/rents, so the user
            //    must complete payment for the earliest stay before moving on.
            var firstPayableStay = pendingStays.First();
            firstPayableStay.IsNextPayable = true;

            var paidFrom = firstPayableStay.FromDate;
            var maxPaidUpto = firstPayableStay.ToDate;

            return Ok(new PaymentContextDto
            {
                TenantId = tenant.TenantId,
                TenantName = tenant.Name,
                PendingAmount = pendingStays.Sum(s => s.PendingAmount),
                PendingStays = pendingStays,
                PaidFrom = paidFrom,
                MaxPaidUpto = maxPaidUpto,
                HasActiveStay = pendingStays.Any(s => s.IsActiveStay),
                AsOfDate = today,
                RoomNumber = firstPayableStay.RoomNumber,
                RentPerMonth = firstPayableStay.RentPerMonth,
                StayType = firstPayableStay.StayType,
                StayStartDate = firstPayableStay.StayStartDate
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET tenant/{tenantId}  — ORIGINAL, fully intact
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("tenant/{tenantId}")]
        public async Task<IActionResult> GetPaymentHistoryForTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var tenantExists = await context.Tenants.AnyAsync(t =>
                t.TenantId == tenantId &&
                t.PgId == pgId &&
                !t.isDeleted
            );

            if (!tenantExists)
                return NotFound("Tenant not found");

            var payments = await context.Payments
                .Include(p => p.PaymentType)
                .Include(p => p.CreatedByUser)
                .Where(p =>
                    p.TenantId == tenantId &&
                    p.PgId == pgId &&
                    !p.IsDeleted
                )
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

            return Ok(payments);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET history  — ✅ CHANGED: multi-select mode + types filter + paymenttype sort
        // ─────────────────────────────────────────────────────────────────────
        [AccessPoint("Payment", "View Payment History")]
        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? mode = null,
            [FromQuery] string? tenantId = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? types = null,
            [FromQuery] string sortBy = "paymentDate",
            [FromQuery] string sortDir = "desc")
        {
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var query = context.Payments
                .AsNoTracking()
                .Include(p => p.Tenant)
                .Include(p => p.CreatedByUser)
                .Include(p => p.PaymentType)
                .Where(p => pgIds.Contains(p.PgId) && !p.IsDeleted);

            // 🔍 Search by receipt no, mobile number, or aadhar number
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.PaymentId.ToLower().Contains(term) ||
                    p.Tenant.ContactNumber.Contains(term) ||
                    p.Tenant.AadharNumber.Contains(term));
            }

            // 🎯 Filters

            // ✅ Mode: multi-select via comma-separated string (e.g. "cash,upi")
            if (!string.IsNullOrEmpty(mode))
            {
                var modeCodes = mode
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(m => m.ToLower())
                    .ToList();

                if (modeCodes.Any())
                    query = query.Where(p => modeCodes.Contains(p.PaymentModeCode.ToLower()));
            }

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(p => p.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(p => p.CreatedByUserId == userId);

            // ✅ Payment type: multi-select via comma-separated codes (e.g. "RENT,ADVANCE_PAYMENT")
            if (!string.IsNullOrEmpty(types))
            {
                var typeCodes = types
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToUpper())
                    .ToList();

                if (typeCodes.Any())
                    query = query.Where(p => typeCodes.Contains(p.PaymentTypeCode));
            }

            // 📊 Total count (before paging)
            var totalCount = await query.CountAsync();

            // ↕ Sorting
            query = sortBy.ToLower() switch
            {
                "tenantname" => sortDir == "asc"
                    ? query.OrderBy(p => p.Tenant.Name)
                    : query.OrderByDescending(p => p.Tenant.Name),

                "amount" => sortDir == "asc"
                    ? query.OrderBy(p => p.Amount)
                    : query.OrderByDescending(p => p.Amount),

                "mode" => sortDir == "asc"
                    ? query.OrderBy(p => p.PaymentModeCode)
                    : query.OrderByDescending(p => p.PaymentModeCode),

                "periodcovered" => sortDir == "asc"
                    ? query.OrderBy(p => p.PaidFrom)
                    : query.OrderByDescending(p => p.PaidFrom),

                // ✅ Sort by payment type name
                "paymenttype" => sortDir == "asc"
                    ? query.OrderBy(p => p.PaymentType.Name)
                    : query.OrderByDescending(p => p.PaymentType.Name),

                _ => sortDir == "asc"
                    ? query.OrderBy(p => p.PaymentDate)
                    : query.OrderByDescending(p => p.PaymentDate)
            };

            // 📄 Paging + projection
            var payments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentHistoryDto
                {
                    PaymentId = p.PaymentId,
                    PaymentType = p.PaymentType.Name,
                    PaymentDate = p.PaymentDate,
                    TenantName = p.Tenant.Name,
                    PeriodCovered =
                        p.PaidFrom.ToString("dd MMM yyyy") + " - " +
                        p.PaidUpto.ToString("dd MMM yyyy"),
                    Amount = p.Amount,
                    Mode = p.PaymentModeCode,
                    CollectedBy = p.CreatedByUser.FullName ?? p.CreatedByUser.UserName!
                })
                .ToListAsync();

            return Ok(new PageResultsDto<PaymentHistoryDto>
            {
                Items = payments,
                TotalCount = totalCount
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE {paymentId}
        // ─────────────────────────────────────────────────────────────────────
        [AccessPoint("Payment", "Delete Payment")]
        [HttpDelete("{paymentId}")]
        public async Task<IActionResult> DeletePayment(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId) || string.IsNullOrEmpty(userId))
                return Unauthorized();

            var payment = await context.Payments
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == paymentId &&
                    p.PgId == pgId &&
                    !p.IsDeleted);

            if (payment == null)
            {
                var otherPgName = await context.Payments
                    .Where(p => p.PaymentId == paymentId && !p.IsDeleted)
                    .Join(context.PGs, p => p.PgId, pg => pg.PgId, (p, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return StatusCode(403, $"This payment belongs to {otherPgName}. Please login to {otherPgName} to modify it.");
                return NotFound("Payment not found");
            }

            // ── Type-specific validation & side-effects ──────────────────────

            if (payment.PaymentTypeCode == "RENT")
            {
                // Only check newer RENT payments (advance payments are not sequential)
                var hasLaterRentPayments = await context.Payments.AnyAsync(p =>
                    p.TenantId == payment.TenantId &&
                    p.PgId == pgId &&
                    !p.IsDeleted &&
                    p.PaymentTypeCode == "RENT" &&
                    p.PaidFrom > payment.PaidFrom
                );

                if (hasLaterRentPayments)
                    return BadRequest("This payment cannot be deleted because newer rent payments exist.");
            }
            else if (payment.PaymentTypeCode == "ADVANCE_PAYMENT")
            {
                // Find the linked advance via the payment notes ("Advance created: {advanceId}")
                var advance = await context.Advances
                    .FirstOrDefaultAsync(a =>
                        a.TenantId == payment.TenantId &&
                        !a.IsSettled &&
                        a.Amount == payment.Amount);

                if (advance == null)
                {
                    // Advance is already settled — cannot delete the original payment
                    var settledAdvance = await context.Advances
                        .AnyAsync(a =>
                            a.TenantId == payment.TenantId &&
                            a.IsSettled &&
                            a.Amount == payment.Amount);

                    if (settledAdvance)
                        return BadRequest("This advance payment cannot be deleted because the advance has already been settled. Delete the settlement first.");

                    // Advance not found at all — allow deletion of orphan payment
                }
                else
                {
                    // Delete the unsettled advance along with the payment
                    context.Advances.Remove(advance);
                }
            }
            else if (payment.PaymentTypeCode == "ADVANCE_REFUND")
            {
                // Reverse the settlement — find the settled advance for this tenant
                var advance = await context.Advances
                    .FirstOrDefaultAsync(a =>
                        a.TenantId == payment.TenantId &&
                        a.IsSettled &&
                        a.SettledDate != null);

                if (advance != null)
                {
                    advance.IsSettled = false;
                    advance.DeductedAmount = 0;
                    advance.SettledDate = null;
                    advance.SettledByUserId = null;
                    advance.Notes = null;
                }
            }

            // ── Soft-delete the payment ──────────────────────────────────────

            payment.IsDeleted = true;
            payment.DeletedAt = DateTime.UtcNow;
            payment.DeletedByUserId = userId;

            context.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid().ToString(),
                PgId = pgId,
                BranchId = payment.BranchId,
                EventType = "PAYMENT_DELETED",
                EntityType = "Payment",
                EntityId = paymentId,
                Description = $"{payment.PaymentTypeCode} payment of ₹{payment.Amount} deleted (Tenant: {payment.TenantId})",
                OldValue = System.Text.Json.JsonSerializer.Serialize(new
                {
                    payment.Amount,
                    payment.PaidFrom,
                    payment.PaidUpto,
                    payment.PaymentModeCode,
                    payment.PaymentTypeCode,
                    payment.TenantId
                }),
                PerformedByUserId = userId,
                PerformedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            return Ok();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET {paymentId}  — ORIGINAL, fully intact
        // ─────────────────────────────────────────────────────────────────────
        [AccessPoint("Payment", "View Payment")]
        [HttpGet("{paymentId}")]
        public async Task<IActionResult> GetPayment(string paymentId)
        {
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var payment = await context.Payments
                .Where(p => p.PaymentId == paymentId && pgIds.Contains(p.PgId) && !p.IsDeleted)
                .Select(p => new
                {
                    p.PaymentId,
                    p.TenantId,
                    p.PaymentDate,
                    p.PaidFrom,
                    p.PaidUpto,
                    p.Amount,
                    p.PaymentModeCode,
                    p.PaymentFrequencyCode,
                    p.Notes
                })
                .FirstOrDefaultAsync();

            if (payment == null)
                return NotFound();

            return Ok(payment);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT {paymentId}  — ORIGINAL, fully intact
        // ─────────────────────────────────────────────────────────────────────
        [AccessPoint("Payment", "Update Payment")]
        [HttpPut("{paymentId}")]
        public async Task<IActionResult> UpdatePayment(string paymentId, UpdatePaymentDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var payment = await context.Payments
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == paymentId &&
                    p.PgId == pgId &&
                    !p.IsDeleted);

            if (payment == null)
            {
                var otherPgName = await context.Payments
                    .Where(p => p.PaymentId == paymentId && !p.IsDeleted)
                    .Join(context.PGs, p => p.PgId, pg => pg.PgId, (p, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return StatusCode(403, $"This payment belongs to {otherPgName}. Please login to {otherPgName} to modify it.");
                return NotFound("Payment not found");
            }

            var hasLaterPayments = await context.Payments.AnyAsync(p =>
                p.TenantId == payment.TenantId &&
                p.PgId == pgId &&
                !p.IsDeleted &&
                p.PaidFrom > payment.PaidFrom
            );

            if (hasLaterPayments)
            {
                if (dto.PaidFrom != payment.PaidFrom ||
                    dto.PaidUpto != payment.PaidUpto)
                {
                    return BadRequest("Changes not saved\nYou can only modify dates for latest payment. Page Refreshed");
                }
            }

            // Capture old values for audit
            var oldAmount = payment.Amount;
            var oldPaidFrom = payment.PaidFrom;
            var oldPaidUpto = payment.PaidUpto;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";

            payment.Amount = dto.Amount;
            payment.PaymentModeCode = dto.PaymentModeCode;
            payment.PaymentFrequencyCode = dto.PaymentFrequencyCode;
            payment.Notes = dto.Notes;

            if (!hasLaterPayments)
            {
                payment.PaidFrom = dto.PaidFrom;
                payment.PaidUpto = dto.PaidUpto;
            }

            // Audit: amount change
            if (oldAmount != dto.Amount)
            {
                context.AuditEvents.Add(new AuditEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    PgId = pgId,
                    BranchId = payment.BranchId,
                    EventType = "PAYMENT_AMOUNT_CHANGED",
                    EntityType = "Payment",
                    EntityId = paymentId,
                    Description = $"Payment amount changed from ₹{oldAmount} to ₹{dto.Amount}",
                    OldValue = System.Text.Json.JsonSerializer.Serialize(new { Amount = oldAmount }),
                    NewValue = System.Text.Json.JsonSerializer.Serialize(new { Amount = dto.Amount }),
                    PerformedByUserId = userId,
                    PerformedAt = DateTime.UtcNow
                });
            }

            // Audit: period change
            if (oldPaidFrom != payment.PaidFrom || oldPaidUpto != payment.PaidUpto)
            {
                context.AuditEvents.Add(new AuditEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    PgId = pgId,
                    BranchId = payment.BranchId,
                    EventType = "PAYMENT_PERIOD_CHANGED",
                    EntityType = "Payment",
                    EntityId = paymentId,
                    Description = $"Payment period changed from {oldPaidFrom:dd MMM yyyy}–{oldPaidUpto:dd MMM yyyy} to {payment.PaidFrom:dd MMM yyyy}–{payment.PaidUpto:dd MMM yyyy}",
                    OldValue = System.Text.Json.JsonSerializer.Serialize(new { PaidFrom = oldPaidFrom, PaidUpto = oldPaidUpto }),
                    NewValue = System.Text.Json.JsonSerializer.Serialize(new { payment.PaidFrom, payment.PaidUpto }),
                    PerformedByUserId = userId,
                    PerformedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();

            return Ok();
        }
    }
}
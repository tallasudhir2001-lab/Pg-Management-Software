using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Payment;
using PgManagement_WebApi.DTOs.Payment.PendingRent;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        public PaymentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
        }
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment(CreatePaymentDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId) || string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1️ Validate tenant
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.TenantId == dto.TenantId &&
                    t.PgId == pgId &&
                    !t.isDeleted);

            if (tenant == null)
                return NotFound("Invalid tenant");

            // 2️ Get all stays ordered
            var stays = await context.TenantRooms
                .Where(tr => tr.TenantId == dto.TenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
                return BadRequest("Tenant has no stay history");

            // 3️ Get last payment (if any)
            var lastPayment = await context.Payments
                .Where(p => p.TenantId == dto.TenantId && p.PgId == pgId)
                .OrderByDescending(p => p.PaidUpto)
                .FirstOrDefaultAsync();

            // 4️ Determine PaidFrom
            DateTime paidFrom;

            if (lastPayment != null)
            {
                paidFrom = lastPayment.PaidUpto.AddDays(1);
            }
            else
            {
                paidFrom = stays.First().FromDate;
            }

            // 5️ Find the stay that contains PaidFrom
            var stay = stays.FirstOrDefault(tr =>
                tr.FromDate <= paidFrom &&
                (tr.ToDate == null || paidFrom <= tr.ToDate.Value));

            if (stay == null)
                return BadRequest("No stay exists for the unpaid period");

            // 6️ Validate PaidUpto
            if (dto.PaidUpto < paidFrom)
                return BadRequest("Paid upto date is before unpaid period");

            if (stay.ToDate.HasValue && dto.PaidUpto > stay.ToDate.Value)
                return BadRequest("Payment exceeds stay duration");

            // 7️ Soft frequency correction
            var frequencyCode = dto.PaymentFrequencyCode;

            if (frequencyCode != "CUSTOM")
            {
                // If UI intent doesn't match actual dates, downgrade
                // (logic optional, safe default)
                // frequencyCode = "CUSTOM";
            }

            // 8 Create payment
            var payment = new Payment
            {
                PaymentId = Guid.NewGuid().ToString(),
                PgId = pgId,
                TenantId = dto.TenantId,
                Amount = dto.Amount,
                PaymentDate = dto.PaymentDate ?? DateTime.UtcNow,
                PaidFrom = paidFrom,
                PaidUpto = dto.PaidUpto,
                PaymentFrequencyCode = frequencyCode,
                PaymentModeCode = dto.PaymentModeCode,
                Notes = dto.Notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            return Ok(new PaymentResponseDto
            {
                PaymentId = payment.PaymentId,
                PaidFrom = payment.PaidFrom,
                PaidUpto = payment.PaidUpto,
                Amount = payment.Amount
            });
        }
        [HttpGet("pending/{tenantId}")]
        public async Task<IActionResult> GetPendingRent(
    string tenantId,
    DateTime? asOfDate = null)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var asOf = (asOfDate ?? DateTime.UtcNow).Date;

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
                .Where(p => p.TenantId == tenantId && p.PgId == pgId)
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
                        rentHistories
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

        private static DateTime Min(DateTime a, DateTime b)
            => a < b ? a : b;

        [HttpGet("context/{tenantId}")]
        public async Task<IActionResult> GetPaymentContext(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var today = DateTime.UtcNow.Date;

            // 1️ Validate tenant
            var tenant = await context.Tenants
                .Where(t => t.TenantId == tenantId && t.PgId == pgId && !t.isDeleted)
                .Select(t => new
                {
                    t.TenantId,
                    t.Name
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound("Invalid tenant");

            // 2️ Load stays
            var stays = await context.TenantRooms
                .Where(tr => tr.TenantId == tenantId && tr.PgId == pgId)
                .OrderBy(tr => tr.FromDate)
                .ToListAsync();

            if (!stays.Any())
                return BadRequest("Tenant has no stay history");

            // 3️ Load last payment
            var lastPayment = await context.Payments
                .Where(p => p.TenantId == tenantId && p.PgId == pgId)
                .OrderByDescending(p => p.PaidUpto)
                .FirstOrDefaultAsync();

            // 4️ Determine PaidFrom
            DateTime paidFrom = lastPayment != null
                ? lastPayment.PaidUpto.AddDays(1)
                : stays.First().FromDate;

            // 5️ Find stay that contains PaidFrom
            var stay = stays.FirstOrDefault(tr =>
                tr.FromDate <= paidFrom &&
                (tr.ToDate == null || paidFrom <= tr.ToDate.Value));

            if (stay == null)
                return BadRequest("No unpaid stay period exists");

            // 6️ Determine MaxPaidUpto
            var maxPaidUpto = stay.ToDate.HasValue
                ? stay.ToDate.Value
                : today;

            // 7️ Get pending amount (reuse logic pattern)
            // We reuse Pending Rent endpoint logic conceptually,
            // but only need the total as-of today.
            decimal pendingAmount = 0;

            // Load payments once
            var payments = await context.Payments
                .Where(p => p.TenantId == tenantId && p.PgId == pgId)
                .ToListAsync();

            // Subtract paid ranges from stay
            var stayFrom = stay.FromDate;
            var stayTo = stay.ToDate.HasValue
                ? Min(stay.ToDate.Value, today)
                : today;

            if (stayFrom <= stayTo)
            {
                var unpaidRanges = DateRangeHelper.Subtract(
                    stayFrom,
                    stayTo,
                    payments.Select(p => (p.PaidFrom, p.PaidUpto))
                );

                if (unpaidRanges.Any())
                {
                    var rentHistories = await context.RoomRentHistories
                        .Where(r =>
                            r.RoomId == stay.RoomId &&
                            r.EffectiveFrom <= stayTo &&
                            (r.EffectiveTo == null || r.EffectiveTo >= stayFrom))
                        .OrderBy(r => r.EffectiveFrom)
                        .ToListAsync();

                    foreach (var range in unpaidRanges)
                    {
                        var slices = RentHelper.GetRentSlices(
                            range.From,
                            range.To,
                            rentHistories
                        );

                        pendingAmount += slices.Sum(s => s.Amount);
                    }
                }
            }

            var roundedPending = Decimal.Round(
     pendingAmount,
     2,
     MidpointRounding.AwayFromZero
 );

            if (roundedPending <= 0)
            {
                return Ok(new PaymentContextDto
                {
                    TenantId = tenant.TenantId,
                    TenantName = tenant.Name,
                    PaidFrom = paidFrom,            // informational (future)
                    MaxPaidUpto = null,             // 👈 IMPORTANT
                    PendingAmount = 0,
                    AsOfDate = today,
                    HasActiveStay = stay.ToDate == null
                });
            }

            // 👇 existing behavior for due payments
            return Ok(new PaymentContextDto
            {
                TenantId = tenant.TenantId,
                TenantName = tenant.Name,
                PaidFrom = paidFrom,
                MaxPaidUpto = maxPaidUpto,
                PendingAmount = roundedPending,
                AsOfDate = today,
                HasActiveStay = stay.ToDate == null
            });

        }
        [HttpGet("tenant/{tenantId}")]
        public async Task<IActionResult> GetPaymentHistoryForTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            // validate tenant belongs to this PG
            var tenantExists = await context.Tenants.AnyAsync(t =>
                t.TenantId == tenantId &&
                t.PgId == pgId &&
                !t.isDeleted
            );

            if (!tenantExists)
                return NotFound("Tenant not found");

            var payments = await context.Payments
                .Where(p =>
                    p.TenantId == tenantId &&
                    p.PgId == pgId
                )
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new TenantPaymentHistoryDto
                {
                    PaymentId = p.PaymentId,

                    PaymentDate = p.PaymentDate,

                    PaidFrom = p.PaidFrom,
                    PaidUpto = p.PaidUpto,

                    Amount = p.Amount,

                    PaymentMode = p.PaymentModeCode,
                    Frequency = p.PaymentFrequencyCode.ToString(),

                    CollectedBy = p.CreatedByUser.UserName
                })
                .ToListAsync();

            return Ok(payments);
        }
        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null,
    [FromQuery] string? mode = null,
    [FromQuery] string? tenantId = null,
    [FromQuery] string? userId = null,
    [FromQuery] string sortBy = "paymentDate",
    [FromQuery] string sortDir = "desc")
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var query = context.Payments
                .AsNoTracking()
                .Include(p => p.Tenant)
                .Include(p => p.CreatedByUser)
                .Where(p => p.PgId == pgId && !p.IsDeleted);

            // 🔍 Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Tenant.Name.ToLower().Contains(term)
                );
            }

            // 🎯 Filters
            if (!string.IsNullOrEmpty(mode))
                query = query.Where(p => p.PaymentModeCode == mode);

            if (!string.IsNullOrEmpty(tenantId))
                query = query.Where(p => p.TenantId == tenantId);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(p => p.CreatedByUserId == userId);

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
                    PaymentDate = p.PaymentDate,
                    TenantName = p.Tenant.Name,
                    PeriodCovered =
                        p.PaidFrom.ToString("dd MMM yyyy") + " - " +
                        p.PaidUpto.ToString("dd MMM yyyy"),
                    Amount = p.Amount,
                    Mode = p.PaymentModeCode,
                    CollectedBy = p.CreatedByUser.UserName!
                })
                .ToListAsync();

            return Ok(new PageResultsDto<PaymentHistoryDto>
            {
                Items = payments,
                TotalCount = totalCount
            });
        }
        [HttpDelete("{paymentId}")]
        public async Task<IActionResult> DeletePayment(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId) || string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1️ Load payment
            var payment = await context.Payments
                .FirstOrDefaultAsync(p =>
                    p.PaymentId == paymentId &&
                    p.PgId == pgId &&
                    !p.IsDeleted);

            if (payment == null)
                return NotFound("Payment not found");

            // 2️ Check if there is any later payment for this tenant
            var hasLaterPayments = await context.Payments.AnyAsync(p =>
                p.TenantId == payment.TenantId &&
                p.PgId == pgId &&
                !p.IsDeleted &&
                p.PaidFrom > payment.PaidFrom
            );

            if (hasLaterPayments)
            {
                return BadRequest(
                    "This payment cannot be deleted because newer payments exist."
                );
            }

            // 3️ Soft delete
            payment.IsDeleted = true;
            payment.DeletedAt = DateTime.UtcNow;
            payment.DeletedByUserId = userId;

            await context.SaveChangesAsync();

            return Ok();
        }

    }
}

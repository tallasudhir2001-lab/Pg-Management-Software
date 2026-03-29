using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.advance;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class AdvanceService:IAdvanceService
    {
        private readonly ApplicationDbContext _context;

        public AdvanceService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<(bool success, object result, int statusCode)> SettleAdvanceAsync(
    string advanceId,
    SettleAdvanceDto dto,
    string pgId,
    string userId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var advance = await _context.Advances
                    .Include(a => a.Tenant)
                    .FirstOrDefaultAsync(a =>
                        a.AdvanceId == advanceId &&
                        !a.IsSettled &&
                        a.Tenant.PgId == pgId);

                if (advance == null)
                    return (false, "Advance not found or already settled", 404);

                // 🔥 VALIDATIONS
                if (dto.DeductedAmount < 0)
                    return (false, "Deduction cannot be negative", 400);

                if (dto.DeductedAmount > advance.Amount)
                    return (false, "Deduction cannot exceed advance", 400);

                var returnAmount = advance.Amount - dto.DeductedAmount;

                // 1️ Update advance
                advance.DeductedAmount = dto.DeductedAmount;
                advance.IsSettled = true;
                advance.SettledDate = DateTime.UtcNow;
                advance.SettledByUserId = userId;
                advance.Notes = dto.Notes;

                // 2️⃣ Create refund payment (ONLY if return > 0)
                if (returnAmount > 0)
                {
                    var payment = new Payment
                    {
                        PaymentId = Guid.NewGuid().ToString(),
                        TenantId = advance.TenantId,
                        PgId = pgId,
                        Amount = -returnAmount, //  money going out
                        PaymentDate = DateTime.UtcNow,
                        PaidFrom = DateTime.UtcNow,
                        PaidUpto = DateTime.UtcNow,
                        PaymentTypeCode = "ADVANCE_REFUND",
                        PaymentModeCode = dto.PaymentModeCode,
                        PaymentFrequencyCode = "ONETIME",
                        CreatedByUserId = userId,
                        Notes = $"Advance refund to Tenant"
                    };

                    _context.Payments.Add(payment);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, new
                {
                    advanceId,
                    advance.Amount,
                    advance.DeductedAmount,
                    ReturnedAmount = returnAmount
                }, 200);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, ex.Message, 500);
            }
        }
        public async Task<(bool success, object result, int statusCode)> CreateAdvanceAsync(
    CreateAdvanceDto dto,
    string pgId,
    string userId,
    string? branchId = null)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1️⃣ Validate tenant
                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t =>
                        t.TenantId == dto.TenantId &&
                        t.PgId == pgId &&
                        !t.isDeleted);

                if (tenant == null)
                    return (false, "Tenant not found", 404);

                // 2️⃣ Check existing active advance
                var hasActiveAdvance = await _context.Advances
                    .AnyAsync(a => a.TenantId == dto.TenantId && !a.IsSettled);

                if (hasActiveAdvance)
                    return (false, "Active advance already exists", 400);

                // 3️⃣ Validation
                if (dto.Amount <= 0)
                    return (false, "Amount must be greater than 0", 400);

                if (string.IsNullOrEmpty(dto.PaymentModeCode))
                    return (false, "Payment mode is required", 400);

                // 4️⃣ Create Advance
                var advance = new Advance
                {
                    AdvanceId = Guid.NewGuid().ToString(),
                    TenantId = dto.TenantId,
                    BranchId = branchId,
                    Amount = dto.Amount,
                    PaidDate = DateTime.UtcNow,
                    IsSettled = false,
                    CreatedByUserId = userId,
                    Notes = dto.Notes
                };

                _context.Advances.Add(advance);

                // 5️⃣ Create Payment
                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid().ToString(),
                    TenantId = dto.TenantId,
                    PgId = pgId,
                    BranchId = branchId,
                    Amount = dto.Amount,
                    PaymentDate = DateTime.UtcNow,
                    PaidFrom = DateTime.UtcNow,
                    PaidUpto = DateTime.UtcNow,
                    PaymentTypeCode = "ADVANCE_PAYMENT",
                    PaymentModeCode = dto.PaymentModeCode,
                    PaymentFrequencyCode= "ONETIME",
                    CreatedByUserId = userId,
                    Notes = $"Advance created: {advance.AdvanceId}"
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, new
                {
                    advance.AdvanceId,
                    advance.Amount,
                    advance.PaidDate
                }, 200);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, ex.Message, 500);
            }
        }

        public async Task<(bool success, object result, int statusCode)> GetAdvancesByTenantAsync(
    string tenantId,
    string pgId)
        {
            var advances = await _context.Advances
                .Include(a => a.Tenant)
                .Where(a => a.TenantId == tenantId && a.Tenant.PgId == pgId)
                .OrderByDescending(a => a.PaidDate)
                .Select(a => new
                {
                    a.AdvanceId,
                    a.Amount,
                    a.DeductedAmount,
                    a.IsSettled,
                    a.PaidDate
                })
                .ToListAsync();

            return (true, advances, 200);
        }
    }
}

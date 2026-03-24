using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Tenant;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class TenantService : ITenantService
    {
        private readonly ApplicationDbContext context;
        private readonly IRoomService roomService;


        public TenantService(ApplicationDbContext context, IRoomService roomService)
        {
            this.context = context;
            this.roomService = roomService;
        }

        public async Task<(bool success, object? result, int statusCode)>
    CreateTenantAsync(CreateTenantDto dto, string pgId, string userId)
        {
            var existingTenant = await FindByAadhaar(pgId, dto.AadharNumber);

            if (existingTenant != null)
            {
                var hasActiveStay = await TenantHasActiveStay(existingTenant.TenantId, pgId);

                if (hasActiveStay)
                {
                    return (false, new
                    {
                        type = "AlreadyActive",
                        tenantId = existingTenant.TenantId
                    }, 409);
                }

                return (false, new
                {
                    type = "TenantExists",
                    tenantId = existingTenant.TenantId,
                    tenantName = existingTenant.Name
                }, 409);
            }

            using var tx = await context.Database.BeginTransactionAsync();

            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid().ToString(),
                PgId = pgId,
                Name = dto.Name,
                ContactNumber = dto.ContactNumber,
                AadharNumber = dto.AadharNumber,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Tenants.Add(tenant);

            //  ADVANCE LOGIC
            if (dto.HasAdvance)
            {
                if (!dto.AdvanceAmount.HasValue || dto.AdvanceAmount <= 0)
                    return (false, "Advance amount required", 400);

                if (string.IsNullOrEmpty(dto.PaymentModeCode))
                    return (false, "Payment mode required", 400);

                var advance = new Advance
                {
                    AdvanceId = Guid.NewGuid().ToString(),
                    TenantId = tenant.TenantId,
                    Amount = dto.AdvanceAmount.Value,
                    PaidDate = DateTime.UtcNow,
                    IsSettled = false,
                    CreatedByUserId = userId
                };

                context.Advances.Add(advance);

                var payment = new Payment
                {
                    PaymentId = Guid.NewGuid().ToString(),
                    TenantId = tenant.TenantId,
                    PgId = pgId,
                    Amount = dto.AdvanceAmount.Value,
                    PaymentDate = DateTime.UtcNow,
                    PaidFrom = DateTime.UtcNow,
                    PaidUpto = DateTime.UtcNow,
                    PaymentTypeCode = "ADVANCE_PAYMENT",
                    PaymentModeCode = dto.PaymentModeCode,
                    PaymentFrequencyCode = "ONETIME",
                    CreatedByUserId = userId
                };

                context.Payments.Add(payment);
            }

            if (!string.IsNullOrEmpty(dto.RoomId))
            {

                var fromDate = dto.FromDate ?? DateTime.UtcNow;

                var (ok, error, status) = await CreateStayInternal(
                    tenant.TenantId,
                    dto.RoomId,
                    fromDate,
                    pgId
                );

                if (!ok)
                    return (false, error, status);
            }
            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, new { tenantId = tenant.TenantId }, 200);
        }


        public async Task<bool> TenantHasActiveStay(string tenantId, string pgId)
        {
            return await context.TenantRooms.AnyAsync(tr =>
                tr.TenantId == tenantId &&
                tr.PgId == pgId &&
                tr.ToDate == null);
        }

        public async Task<Tenant?> FindByAadhaar(string pgId, string aadhaar)
        {
            if (string.IsNullOrWhiteSpace(aadhaar))
                return null;

            return await context.Tenants.FirstOrDefaultAsync(t =>
                t.PgId == pgId &&
                t.AadharNumber == aadhaar &&
                !t.isDeleted);
        }
        public async Task<(bool success, object? result, int statusCode)> CreateStayAsync(
    string tenantId,
    CreateStayDto dto,
    string pgId)
        {
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.TenantId == tenantId &&
                    t.PgId == pgId &&
                    !t.isDeleted);

            if (tenant == null)
                return (false, "Tenant not found.", 404);

            var hasActiveStay = await context.TenantRooms.AnyAsync(tr =>
                tr.TenantId == tenantId &&
                tr.PgId == pgId &&
                tr.ToDate == null);

            if (hasActiveStay)
                return (false, "Tenant already has an active stay.", 409);

            using var tx = await context.Database.BeginTransactionAsync();

            var fromDate = dto.FromDate ?? DateTime.UtcNow;

            var (ok, error, status) = await CreateStayInternal(
                tenantId,
                dto.RoomId,
                fromDate,
                pgId
            );

            if (!ok)
                return (false, error, status);

            tenant.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, new { tenantId }, 200);
        }

        public async Task<(bool success, string? error, int statusCode)> CreateStayInternal(
    string tenantId,
    string roomId,
    DateTime fromDate,
    string pgId)
        {
            // 1️⃣ Validate room
            var (valid, error, status) = await roomService.ValidateRoomAsync(roomId, pgId);
            if (!valid)
                return (false, error, status);

            // 2️⃣ Get rent
            var (rentOk, rentId, rentError, rentStatus) =
                await roomService.GetActiveRentAsync(roomId);

            if (!rentOk)
                return (false, rentError, rentStatus);

            // 3️⃣ Create TenantRoom
            context.TenantRooms.Add(new TenantRoom
            {
                TenantRoomId = Guid.NewGuid(),
                TenantId = tenantId,
                RoomId = roomId,
                PgId = pgId,
                FromDate = fromDate
            });

            // 4️⃣ Create TenantRentHistory
            context.TenantRentHistories.Add(new TenantRentHistory
            {
                TenantRentHistoryId = Guid.NewGuid(),
                TenantId = tenantId,
                RoomRentHistoryId = rentId,
                FromDate = fromDate
            });

            return (true, null, 200);
        }

        public async Task<(bool success, object? result, int statusCode)> ChangeRoomAsync(
    string tenantId,
    string newRoomId,
    string pgId, DateTime changeDate)
        {
            // 1️⃣ Get active stay
            var activeAssignment = await context.TenantRooms
                .FirstOrDefaultAsync(tr =>
                    tr.TenantId == tenantId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null);

            if (activeAssignment == null)
                return (false, "Tenant does not have an active room.", 400);

            if (activeAssignment.RoomId == newRoomId)
                return (false, "Tenant is already in this room.", 400);

            var roomChangeDate = changeDate.Date;
            var previousDay = roomChangeDate.AddDays(-1);
            var currentStayStart = activeAssignment.FromDate.Date;

            if (roomChangeDate < currentStayStart)
                return (false, "Change date cannot be before current stay start date.", 400);

            using var tx = await context.Database.BeginTransactionAsync();

            

            // 2️⃣ Close old stay
            activeAssignment.ToDate = previousDay;

            // 3️⃣ Close active rent
            var activeRent = await context.TenantRentHistories
                .FirstOrDefaultAsync(trh =>
                    trh.TenantId == tenantId &&
                    trh.ToDate == null);

            if (activeRent != null)
            {
                activeRent.ToDate = previousDay;
            }

            // 4️⃣ Create new stay using shared method
            var (ok, error, status) = await CreateStayInternal(
                tenantId,
                newRoomId,
                roomChangeDate,
                pgId
            );

            if (!ok)
                return (false, error, status);

            // 5️⃣ Update tenant metadata
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.TenantId == tenantId &&
                    t.PgId == pgId &&
                    !t.isDeleted);

            if (tenant != null)
            {
                tenant.UpdatedAt = DateTime.Now;
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return (true, null, 204);
        }

    }
}

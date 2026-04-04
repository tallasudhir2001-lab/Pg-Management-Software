using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Tenant;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class TenantService : ITenantService
    {
        private readonly ApplicationDbContext context;
        private readonly IRoomService roomService;
        private readonly ILogger<TenantService> _logger;

        public TenantService(ApplicationDbContext context, IRoomService roomService, ILogger<TenantService> logger)
        {
            this.context = context;
            this.roomService = roomService;
            _logger = logger;
        }

        public async Task<(bool success, object? result, int statusCode)>
    CreateTenantAsync(CreateTenantDto dto, string pgId, string userId, string? branchId = null)
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
                BranchId = branchId,
                Name = dto.Name,
                ContactNumber = dto.ContactNumber,
                AadharNumber = dto.AadharNumber,
                Notes = dto.Notes,
                Email = dto.Email,
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
                    BranchId = branchId,
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
                    BranchId = branchId,
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
                    pgId,
                    dto.StayType ?? "MONTHLY"
                );

                if (!ok)
                    return (false, error, status);
            }
            await context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation("Tenant {TenantId} created in PG {PgId}", tenant.TenantId, pgId);
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
                pgId,
                dto.StayType ?? "MONTHLY"
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
    string pgId,
    string stayType = "MONTHLY")
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
                FromDate = fromDate,
                StayType = stayType?.ToUpper() == "DAILY" ? "DAILY" : "MONTHLY"
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
    string pgId, DateTime changeDate,
    string stayType = "MONTHLY")
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
                pgId,
                activeAssignment.StayType
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

        public async Task<(bool success, object? result, int statusCode)> ChangeStayTypeAsync(
            string tenantId,
            string newStayType,
            string pgId,
            DateTime effectiveDate)
        {
            var activeAssignment = await context.TenantRooms
                .FirstOrDefaultAsync(tr =>
                    tr.TenantId == tenantId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null);

            if (activeAssignment == null)
                return (false, "Tenant does not have an active stay.", 400);

            if (activeAssignment.StayType == newStayType)
                return (false, $"Tenant is already on {newStayType} stay.", 400);

            var changeDate = effectiveDate.Date;
            var previousDay = changeDate.AddDays(-1);

            if (changeDate < activeAssignment.FromDate.Date)
                return (false, "Effective date cannot be before current stay start date.", 400);

            using var tx = await context.Database.BeginTransactionAsync();

            // Close current stay
            activeAssignment.ToDate = previousDay;

            // Close active rent history
            var activeRent = await context.TenantRentHistories
                .FirstOrDefaultAsync(trh =>
                    trh.TenantId == tenantId &&
                    trh.ToDate == null);

            if (activeRent != null)
            {
                activeRent.ToDate = previousDay;
            }

            // Create new stay in the same room with new stay type
            var (ok, error, status) = await CreateStayInternal(
                tenantId,
                activeAssignment.RoomId,
                changeDate,
                pgId,
                newStayType
            );

            if (!ok)
                return (false, error, status);

            // Update tenant metadata
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

        public async Task<PageResultsDto<TenantListDto>> GetTenantsAsync(
            List<string> pgIds, int page, int pageSize, string? search, string? status,
            string? roomId, bool? rentPending, bool? advancePending, bool? overdueCheckout,
            string sortBy, string sortDir)
        {
            var today = DateTime.Now.Date;

            IQueryable<Tenant> query = context.Tenants
                .AsNoTracking()
                .Where(t => pgIds.Contains(t.PgId) && !t.isDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.Name.Contains(search) ||
                    t.ContactNumber.Contains(search) ||
                    t.AadharNumber.Contains(search));
            }

            query = (sortBy.ToLower(), sortDir.ToLower()) switch
            {
                ("updated", "desc") => query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt),
                ("updated", "asc") => query.OrderBy(t => t.UpdatedAt ?? t.CreatedAt),
                ("name", "desc") => query.OrderByDescending(t => t.Name),
                ("name", "asc") => query.OrderBy(t => t.Name),
                _ => query.OrderBy(t => t.Name)
            };

            var tenants = await query
                .Select(t => new { t.TenantId, t.Name, t.ContactNumber })
                .ToListAsync();

            var tenantIds = tenants.Select(t => t.TenantId).ToList();

            var tenantRooms = await context.TenantRooms
                .AsNoTracking()
                .Where(tr => tenantIds.Contains(tr.TenantId))
                .Select(tr => new TenantRoomDto
                {
                    TenantId = tr.TenantId,
                    RoomId = tr.RoomId,
                    FromDate = tr.FromDate,
                    ToDate = tr.ToDate,
                    RoomNumber = tr.Room.RoomNumber,
                    StayType = tr.StayType
                })
                .ToListAsync();

            var payments = await context.Payments
                .AsNoTracking()
                .Where(p => tenantIds.Contains(p.TenantId) && !p.IsDeleted)
                .Select(p => new
                {
                    p.TenantId,
                    PaidFrom = p.PaidFrom.Date,
                    PaidUpto = p.PaidUpto.Date,
                    p.PaymentTypeCode
                })
                .ToListAsync();

            var roomsLookup = tenantRooms.GroupBy(tr => tr.TenantId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var paymentsLookup = payments.GroupBy(p => p.TenantId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<TenantListDto>();

            foreach (var tenant in tenants)
            {
                var stays = roomsLookup.ContainsKey(tenant.TenantId)
                    ? roomsLookup[tenant.TenantId] : new List<TenantRoomDto>();

                var activeStay = stays.FirstOrDefault(s => s.ToDate == null);
                var lastStay = stays.Where(s => s.ToDate != null).OrderByDescending(s => s.ToDate).FirstOrDefault();

                var tenantPaymentList = paymentsLookup.ContainsKey(tenant.TenantId)
                    ? paymentsLookup[tenant.TenantId] : [];

                var tenantPayments = tenantPaymentList
                    .Where(p => p.PaymentTypeCode == "RENT")
                    .Select(p => (p.PaidFrom, p.PaidUpto));

                var lastPaymentDate = tenantPaymentList
                    .Where(p => p.PaymentTypeCode != "ADVANCE_PAYMENT")
                    .Select(p => (DateTime?)p.PaidUpto)
                    .DefaultIfEmpty().Max();

                bool hasPending = false;
                var allUnpaidRanges = new List<(DateTime From, DateTime To)>();

                foreach (var stay in stays)
                {
                    var stayFrom = stay.FromDate.Date;
                    var stayTo = stay.ToDate ?? today;
                    var unpaid = DateRangeHelper.Subtract(stayFrom, stayTo, tenantPayments);
                    allUnpaidRanges.AddRange(unpaid);
                    if (unpaid.Any()) hasPending = true;
                }

                DateTime? overdueSince = allUnpaidRanges.Any() ? allUnpaidRanges.Min(r => r.From) : null;
                int? daysOverdue = overdueSince.HasValue ? (today - overdueSince.Value).Days + 1 : null;

                var roomIdValue = activeStay?.RoomId ?? lastStay?.RoomId;
                var roomNumber = activeStay != null ? activeStay.RoomNumber
                    : lastStay != null ? lastStay.RoomNumber + " (ex)" : null;
                var checkedInAt = activeStay?.FromDate ?? lastStay?.FromDate;

                result.Add(new TenantListDto
                {
                    TenantId = tenant.TenantId,
                    Name = tenant.Name,
                    ContactNumber = tenant.ContactNumber,
                    RoomId = roomIdValue,
                    RoomNumber = roomNumber,
                    CheckedInAt = checkedInAt,
                    Status = activeStay != null ? "ACTIVE"
                           : stays.Any() ? "MOVED OUT" : "NO STAY",
                    IsRentPending = activeStay != null || stays.Any() ? hasPending : false,
                    LastPaymentDate = lastPaymentDate,
                    OverdueSince = activeStay != null ? overdueSince : null,
                    DaysOverdue = activeStay != null ? daysOverdue : null,
                    StayType = activeStay?.StayType ?? lastStay?.StayType ?? "MONTHLY"
                });
            }

            // Post-filters
            if (!string.IsNullOrEmpty(status))
            {
                result = status.ToLower().Replace(" ", "") switch
                {
                    "active" => result.Where(x => x.Status == "ACTIVE").ToList(),
                    "movedout" => result.Where(x => x.Status == "MOVED OUT").ToList(),
                    "nostay" => result.Where(x => x.Status == "NO STAY").ToList(),
                    _ => result
                };
            }

            if (!string.IsNullOrEmpty(roomId))
                result = result.Where(x => x.RoomId == roomId).ToList();

            if (rentPending.HasValue)
                result = result.Where(x => x.IsRentPending == rentPending.Value).ToList();

            if (advancePending.HasValue && advancePending.Value)
            {
                var tenantIdsWithUnsettledAdvance = await context.Advances
                    .AsNoTracking()
                    .Where(a => !a.IsSettled && !a.IsDeleted)
                    .Select(a => a.TenantId).Distinct().ToListAsync();
                var unsettledSet = tenantIdsWithUnsettledAdvance.ToHashSet();
                result = result.Where(x => unsettledSet.Contains(x.TenantId)).ToList();
            }

            if (overdueCheckout.HasValue && overdueCheckout.Value)
            {
                try
                {
                    var overdueCheckoutTenantIds = await context.TenantRooms
                        .AsNoTracking()
                        .Where(tr => tr.ToDate == null && tr.ExpectedCheckOutDate != null && tr.ExpectedCheckOutDate.Value < today)
                        .Select(tr => tr.TenantId).Distinct().ToListAsync();
                    var overdueSet = overdueCheckoutTenantIds.ToHashSet();
                    result = result.Where(x => overdueSet.Contains(x.TenantId)).ToList();
                }
                catch { }
            }

            if (sortBy.Equals("daysoverdue", StringComparison.OrdinalIgnoreCase))
            {
                result = sortDir == "asc"
                    ? result.OrderBy(x => x.DaysOverdue ?? -1).ToList()
                    : result.OrderByDescending(x => x.DaysOverdue ?? -1).ToList();
            }

            var totalCount = result.Count;
            var pagedResult = result.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new PageResultsDto<TenantListDto>
            {
                Items = pagedResult,
                TotalCount = totalCount
            };
        }

        public async Task<object?> GetTenantByIdAsync(string tenantId, List<string> pgIds)
        {
            var tenant = await context.Tenants
                .Where(t => t.TenantId == tenantId && pgIds.Contains(t.PgId) && !t.isDeleted)
                .Select(t => new
                {
                    t.TenantId, t.Name, t.ContactNumber, t.AadharNumber, t.Notes, t.Email,
                    ActiveAssignment = context.TenantRooms
                        .Where(tr => tr.TenantId == tenantId && tr.ToDate == null)
                        .Select(tr => new { tr.Room.RoomNumber, tr.FromDate })
                        .FirstOrDefault(),
                    LastAssignment = context.TenantRooms
                        .Where(tr => tr.TenantId == tenantId)
                        .OrderByDescending(tr => tr.ToDate ?? DateTime.UtcNow)
                        .Select(tr => new { tr.Room.RoomNumber, tr.FromDate, tr.ToDate })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (tenant == null) return null;

            object stays;
            try
            {
                stays = await context.TenantRooms
                    .Where(tr => tr.TenantId == tenantId && pgIds.Contains(tr.PgId))
                    .OrderBy(tr => tr.FromDate)
                    .Select(tr => new
                    {
                        tr.RoomId, RoomNumber = tr.Room.RoomNumber,
                        tr.FromDate, tr.ToDate, tr.StayType, tr.ExpectedCheckOutDate
                    })
                    .ToListAsync();
            }
            catch
            {
                stays = await context.TenantRooms
                    .Where(tr => tr.TenantId == tenantId && pgIds.Contains(tr.PgId))
                    .OrderBy(tr => tr.FromDate)
                    .Select(tr => new
                    {
                        tr.RoomId, RoomNumber = tr.Room.RoomNumber,
                        tr.FromDate, tr.ToDate, tr.StayType
                    })
                    .ToListAsync();
            }

            var advances = await context.Advances
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.PaidDate)
                .Select(a => new { a.AdvanceId, a.Amount, a.DeductedAmount, a.IsSettled, a.PaidDate })
                .ToListAsync();

            var advance = await context.Advances
                .Where(a => a.TenantId == tenantId && !a.IsSettled)
                .Select(a => new { a.Amount })
                .FirstOrDefaultAsync();

            var advancePayment = await context.Payments
                .Where(p => p.TenantId == tenantId && p.PaymentTypeCode == "ADVANCE_PAYMENT" && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new { p.PaymentModeCode })
                .FirstOrDefaultAsync();

            return new
            {
                tenant.TenantId, tenant.Name, tenant.ContactNumber, tenant.AadharNumber,
                tenant.Email,
                HasAdvance = advance != null,
                AdvanceAmount = advance != null ? advance.Amount : (decimal?)null,
                AdvancePaymentMode = advancePayment != null ? advancePayment.PaymentModeCode : null,
                Advances = advances,
                tenant.Notes,
                RoomNumber = tenant.ActiveAssignment != null ? tenant.ActiveAssignment.RoomNumber
                    : tenant.LastAssignment != null ? tenant.LastAssignment.RoomNumber + " (previous room)" : null,
                CheckedInAt = tenant.ActiveAssignment != null ? (DateTime?)tenant.ActiveAssignment.FromDate
                    : tenant.LastAssignment != null ? tenant.LastAssignment.FromDate : null,
                MovedOutAt = tenant.ActiveAssignment == null && tenant.LastAssignment != null
                    ? (DateTime?)tenant.LastAssignment.ToDate : null,
                Status = tenant.ActiveAssignment != null ? "ACTIVE"
                    : tenant.LastAssignment != null ? "MOVED OUT" : "NO STAY",
                Stays = stays
            };
        }

        public async Task<(bool success, object result, int statusCode)> SetExpectedCheckOutAsync(
            string tenantId, string pgId, SetExpectedCheckOutDto dto)
        {
            var activeRoom = await context.TenantRooms
                .FirstOrDefaultAsync(tr => tr.TenantId == tenantId && tr.PgId == pgId && tr.ToDate == null);

            if (activeRoom == null)
                return (false, "Tenant does not have an active stay.", 400);

            if (dto.ExpectedCheckOutDate.HasValue && dto.ExpectedCheckOutDate.Value.Date <= DateTime.UtcNow.Date)
                return (false, "Expected checkout date must be in the future.", 400);

            activeRoom.ExpectedCheckOutDate = dto.ExpectedCheckOutDate?.Date;
            await context.SaveChangesAsync();
            return (true, "OK", 204);
        }

        public async Task<(bool success, object result, int statusCode)> MoveOutTenantAsync(
            string tenantId, string pgId, MoveOutDto dto)
        {
            var moveOutDate = dto.MoveOutDate.Date;

            using var tx = await context.Database.BeginTransactionAsync();

            var activeRoom = await context.TenantRooms
                .FirstOrDefaultAsync(tr => tr.TenantId == tenantId && tr.PgId == pgId && tr.ToDate == null);

            if (activeRoom == null)
            {
                var otherPgName = await context.TenantRooms
                    .Where(tr => tr.TenantId == tenantId && tr.ToDate == null)
                    .Join(context.PGs, tr => tr.PgId, pg => pg.PgId, (tr, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return (false, $"This tenant belongs to {otherPgName}. Please login to {otherPgName} to modify it.", 403);
                return (false, "Tenant does not have an active room.", 400);
            }

            var latestPayment = await context.Payments
                .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                .OrderByDescending(p => p.PaidUpto)
                .FirstOrDefaultAsync();

            if (latestPayment != null && latestPayment.PaidUpto.Date > moveOutDate)
                return (false,
                    $"Move-out date cannot be before the latest payment's paid-up date ({latestPayment.PaidUpto:dd MMM yyyy}). Please select a date on or after that.",
                    400);

            activeRoom.ToDate = moveOutDate;
            activeRoom.ExpectedCheckOutDate = null;

            var activeRent = await context.TenantRentHistories
                .FirstOrDefaultAsync(trh => trh.TenantId == tenantId && trh.ToDate == null);

            if (activeRent != null)
                activeRent.ToDate = moveOutDate;

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PgId == pgId && !t.isDeleted);
            if (tenant != null)
                tenant.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            _logger.LogInformation("Tenant {TenantId} moved out of PG {PgId}", tenantId, pgId);
            return (true, "OK", 204);
        }

        public async Task<(bool success, object result, int statusCode)> UpdateTenantAsync(
            string tenantId, string pgId, UpdateTenantDto dto)
        {
            var tenant = await context.Tenants
                .SingleOrDefaultAsync(t => t.TenantId == tenantId && t.PgId == pgId);
            if (tenant == null)
            {
                var otherPgName = await context.Tenants
                    .Where(t => t.TenantId == tenantId && !t.isDeleted)
                    .Join(context.PGs, t => t.PgId, pg => pg.PgId, (t, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return (false, $"This tenant belongs to {otherPgName}. Please login to {otherPgName} to modify it.", 403);
                return (false, "Not found", 404);
            }

            tenant.Name = dto.Name;
            tenant.ContactNumber = dto.ContactNumber;
            tenant.AadharNumber = dto.AadharNumber;
            tenant.Notes = dto.Notes;
            tenant.Email = dto.Email;
            tenant.UpdatedAt = DateTime.Now;

            await context.SaveChangesAsync();
            return (true, "OK", 204);
        }

        public async Task<(bool success, object result, int statusCode)> DeleteTenantAsync(
            string tenantId, string pgId)
        {
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PgId == pgId && !t.isDeleted);

            if (tenant == null)
            {
                var otherPgName = await context.Tenants
                    .Where(t => t.TenantId == tenantId && !t.isDeleted)
                    .Join(context.PGs, t => t.PgId, pg => pg.PgId, (t, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return (false, $"This tenant belongs to {otherPgName}. Please login to {otherPgName} to modify it.", 403);
                return (false, "Not found", 404);
            }

            var hasActiveRoom = await context.TenantRooms.AnyAsync(tr =>
                tr.TenantId == tenantId && tr.PgId == pgId && tr.ToDate == null);
            if (hasActiveRoom)
                return (false, "Active tenant cannot be deleted. Move out first.", 409);

            var hasActiveRent = await context.TenantRentHistories.AnyAsync(trh =>
                trh.TenantId == tenantId && trh.ToDate == null);
            if (hasActiveRent)
                return (false, "Tenant has active rent configuration. Move out first.", 409);

            tenant.isDeleted = true;
            tenant.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            _logger.LogInformation("Tenant {TenantId} deleted from PG {PgId}", tenantId, pgId);
            return (true, "OK", 204);
        }

        public async Task<object?> FindByAadharNumberAsync(string pgId, string aadhar)
        {
            return await context.Tenants
                .Where(t => t.PgId == pgId && t.AadharNumber == aadhar && !t.isDeleted)
                .Select(t => new { tenantId = t.TenantId, name = t.Name, contactNumber = t.ContactNumber })
                .FirstOrDefaultAsync();
        }

    }
}

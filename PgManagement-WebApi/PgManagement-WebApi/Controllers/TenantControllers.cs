using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Tenant;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        private readonly ITenantService tenantService;

        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration, ITenantService tenantService)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
            this.tenantService = tenantService;
        }
        [AccessPoint("Tenant", "View All Tenants")]
        [HttpGet]
        public async Task<IActionResult> GetTenants(
     int page = 1,
     int pageSize = 10,
     string? search = null,
     string? status = null,
     string? roomId = null,
     bool? rentPending = null,
     bool? advancePending = null,
     string sortBy = "updated",
     string sortDir = "desc")
        {
            var today = DateTime.UtcNow.Date;

            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            // 1️⃣ Base Query
            IQueryable<Tenant> query = context.Tenants
                .AsNoTracking()
                .Where(t => pgIds.Contains(t.PgId) && !t.isDeleted);

            //  Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.Name.Contains(search) ||
                    t.ContactNumber.Contains(search) ||
                    t.AadharNumber.Contains(search));
            }

            //  Sorting
            query = (sortBy.ToLower(), sortDir.ToLower()) switch
            {
                ("updated", "desc") => query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt),
                ("updated", "asc") => query.OrderBy(t => t.UpdatedAt ?? t.CreatedAt),
                ("name", "desc") => query.OrderByDescending(t => t.Name),
                ("name", "asc") => query.OrderBy(t => t.Name),
                _ => query.OrderBy(t => t.Name)
            };

            // 2️⃣ Fetch ALL matching tenants (pagination applied after filtering)
            var tenants = await query
                .Select(t => new
                {
                    t.TenantId,
                    t.Name,
                    t.ContactNumber
                })
                .ToListAsync();

            var tenantIds = tenants.Select(t => t.TenantId).ToList();

            // 3️ Fetch related data (optimized)
            var tenantRooms = await context.TenantRooms
                .AsNoTracking()
                .Where(tr => tenantIds.Contains(tr.TenantId))
                .Select(tr => new TenantRoomDto
                {
                    TenantId = tr.TenantId,
                    RoomId = tr.RoomId,
                    FromDate = tr.FromDate,
                    ToDate = tr.ToDate,
                    RoomNumber = tr.Room.RoomNumber
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

            // 4️ Group for O(1) lookup
            var roomsLookup = tenantRooms
                .GroupBy(tr => tr.TenantId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var paymentsLookup = payments
                .GroupBy(p => p.TenantId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<TenantListDto>();

            // 5️ Process each tenant
            foreach (var tenant in tenants)
            {
                var stays = roomsLookup.ContainsKey(tenant.TenantId)
             ? roomsLookup[tenant.TenantId]
             : new List<TenantRoomDto>();


                var activeStay = stays.FirstOrDefault(s => s.ToDate == null);

                var lastStay = stays
                    .Where(s => s.ToDate != null)
                    .OrderByDescending(s => s.ToDate)
                    .FirstOrDefault();

                var tenantPaymentList = paymentsLookup.ContainsKey(tenant.TenantId)
                    ? paymentsLookup[tenant.TenantId]
                    : [];

                var tenantPayments = tenantPaymentList.Select(p => (p.PaidFrom, p.PaidUpto));

                var lastPaymentDate = tenantPaymentList
                    .Where(p => p.PaymentTypeCode != "ADVANCE_PAYMENT")
                    .Select(p => (DateTime?)p.PaidUpto)
                    .DefaultIfEmpty()
                    .Max();

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

                var roomNumber = activeStay != null
                    ? activeStay.RoomNumber
                    : lastStay != null
                        ? lastStay.RoomNumber + " (ex)"
                        : null;

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
                           : stays.Any() ? "MOVED OUT"
                           : "NO STAY",
                    IsRentPending = activeStay != null || stays.Any() ? hasPending : false,
                    LastPaymentDate = lastPaymentDate,
                    OverdueSince = activeStay != null ? overdueSince : null,
                    DaysOverdue = activeStay != null ? daysOverdue : null
                });
            }

            // 6️⃣ Filters — applied before pagination
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
            {
                result = result.Where(x => x.RoomId == roomId).ToList();
            }

            if (rentPending.HasValue)
            {
                result = result.Where(x => x.IsRentPending == rentPending.Value).ToList();
            }

            if (advancePending.HasValue && advancePending.Value)
            {
                var tenantIdsWithUnsettledAdvance = await context.Advances
                    .AsNoTracking()
                    .Where(a => !a.IsSettled && !a.IsDeleted)
                    .Select(a => a.TenantId)
                    .Distinct()
                    .ToListAsync();

                var unsettledSet = tenantIdsWithUnsettledAdvance.ToHashSet();
                result = result.Where(x => unsettledSet.Contains(x.TenantId)).ToList();
            }

            // Sort by computed fields (post-processing)
            if (sortBy.Equals("daysoverdue", StringComparison.OrdinalIgnoreCase))
            {
                result = sortDir == "asc"
                    ? result.OrderBy(x => x.DaysOverdue ?? -1).ToList()
                    : result.OrderByDescending(x => x.DaysOverdue ?? -1).ToList();
            }

            // 7️⃣ totalCount is now post-filter for accurate pagination
            var totalCount = result.Count;

            // 8️⃣ Paginate after filtering
            var pagedResult = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PageResultsDto<TenantListDto>
            {
                Items = pagedResult,
                TotalCount = totalCount
            });
        }


        [AccessPoint("Tenant", "View Tenant Details")]
        [HttpGet("{tenantId}")]
        public async Task<IActionResult> GetTenantById(string tenantId)
        {
            var pgIds = await this.GetEffectivePgIds(context);
            if (!pgIds.Any())
                return Unauthorized();

            var tenant = await context.Tenants
                .Where(t =>
                    t.TenantId == tenantId &&
                    pgIds.Contains(t.PgId) &&
                    !t.isDeleted)
                .Select(t => new
                {
                    t.TenantId,
                    t.Name,
                    t.ContactNumber,
                    t.AadharNumber,
                    t.Notes,
                    t.Email,

                    //  Active stay (if any)
                    ActiveAssignment = context.TenantRooms
                        .Where(tr =>
                            tr.TenantId == tenantId &&
                            tr.ToDate == null)
                        .Select(tr => new
                        {
                            tr.Room.RoomNumber,
                            tr.FromDate
                        })
                        .FirstOrDefault(),

                    //  Last stay (for moved-out tenants)
                    LastAssignment = context.TenantRooms
                        .Where(tr =>
                            tr.TenantId == tenantId)
                        .OrderByDescending(tr => tr.ToDate ?? DateTime.UtcNow)
                        .Select(tr => new
                        {
                            tr.Room.RoomNumber,
                            tr.FromDate,
                            tr.ToDate
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound();

            var stays = await context.TenantRooms
                            .Where(tr => tr.TenantId == tenantId && pgIds.Contains(tr.PgId))
                            .OrderBy(tr => tr.FromDate)
                            .Select(tr => new
                            {
                                tr.RoomId,
                                RoomNumber = tr.Room.RoomNumber,
                                tr.FromDate,
                                tr.ToDate
                            })
                            .ToListAsync();
            var advances = await context.Advances
                            .Where(a => a.TenantId == tenantId)
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

            var advance = await context.Advances
                            .Where(a => a.TenantId == tenantId && !a.IsSettled)
                            .Select(a => new
                            {
                                a.Amount
                            })
                            .FirstOrDefaultAsync();

            var advancePayment = await context.Payments
                            .Where(p =>
                                p.TenantId == tenantId &&
                                p.PaymentTypeCode == "ADVANCE_PAYMENT" &&
                                !p.IsDeleted)
                            .OrderByDescending(p => p.CreatedAt)
                            .Select(p => new
                            {
                                p.PaymentModeCode
                            })
                            .FirstOrDefaultAsync();


            return Ok(new
            {
                tenant.TenantId,
                tenant.Name,
                tenant.ContactNumber,
                tenant.AadharNumber,
                tenant.Email,
                HasAdvance = advance != null,
                AdvanceAmount = advance != null ? advance.Amount : (decimal?)null,
                AdvancePaymentMode = advancePayment != null ? advancePayment.PaymentModeCode : null,
                Advances = advances,
                tenant.Notes,

                //  EF-safe conditional mapping
                RoomNumber =
                    tenant.ActiveAssignment != null
                        ? tenant.ActiveAssignment.RoomNumber
                        : tenant.LastAssignment != null
                            ? tenant.LastAssignment.RoomNumber + " (previous room)"
                            : null,

                CheckedInAt =
                     tenant.ActiveAssignment != null
                        ? (DateTime?)tenant.ActiveAssignment.FromDate
                        : tenant.LastAssignment != null
                        ? tenant.LastAssignment.FromDate
                        : null,

                MovedOutAt =
                     tenant.ActiveAssignment == null && tenant.LastAssignment != null
                        ? (DateTime?)tenant.LastAssignment.ToDate
                        : null,


                Status = tenant.ActiveAssignment != null
                    ? "ACTIVE"
                    : tenant.LastAssignment != null
                        ? "MOVED OUT"
                        : "NO STAY",

                Stays = stays
            });
        }


        [AccessPoint("Tenant", "Change Tenant Room")]
        [HttpPost("{tenantId}/change-room")]
        public async Task<IActionResult> ChangeRoom(string tenantId, ChangeRoomDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var (success, result, statusCode) =
                await tenantService.ChangeRoomAsync(tenantId, dto.newRoomId, pgId, dto.changeDate);

            if (!success)
                return StatusCode(statusCode, result);

            return NoContent();
        }

        [AccessPoint("Tenant", "Move Out Tenant")]
        [HttpPost("{tenantId}/move-out")]
        public async Task<IActionResult> MoveOutTenant(string tenantId, [FromBody] MoveOutDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var moveOutDate = dto.MoveOutDate.Date;

            using var tx = await context.Database.BeginTransactionAsync();

            // 1️⃣ Close active room assignment
            var activeRoom = await context.TenantRooms
                .FirstOrDefaultAsync(tr =>
                    tr.TenantId == tenantId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null);

            if (activeRoom == null)
            {
                var otherPgName = await context.TenantRooms
                    .Where(tr => tr.TenantId == tenantId && tr.ToDate == null)
                    .Join(context.PGs, tr => tr.PgId, pg => pg.PgId, (tr, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return StatusCode(403, $"This tenant belongs to {otherPgName}. Please login to {otherPgName} to modify it.");
                return BadRequest("Tenant does not have an active room.");
            }

            // 2️⃣ Validate move-out date against latest payment's PaidUpto
            var latestPayment = await context.Payments
                .Where(p => p.TenantId == tenantId && !p.IsDeleted)
                .OrderByDescending(p => p.PaidUpto)
                .FirstOrDefaultAsync();

            if (latestPayment != null && latestPayment.PaidUpto.Date > moveOutDate)
                return BadRequest(
                    $"Move-out date cannot be before the latest payment's paid-up date ({latestPayment.PaidUpto:dd MMM yyyy}). Please select a date on or after that.");

            activeRoom.ToDate = moveOutDate;

            // 3️⃣ Close active rent history
            var activeRent = await context.TenantRentHistories
                .FirstOrDefaultAsync(trh =>
                    trh.TenantId == tenantId &&
                    trh.ToDate == null);

            if (activeRent != null)
            {
                activeRent.ToDate = moveOutDate;
            }

            // 4️⃣ Update tenant metadata
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.TenantId == tenantId &&
                    t.PgId == pgId &&
                    !t.isDeleted);

            if (tenant != null)
            {
                tenant.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return NoContent();
        }

        [AccessPoint("Tenant", "Create Tenant")]
        [HttpPost("create-tenant")]
        public async Task<IActionResult> CreateTenant(CreateTenantDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var (success, result, statusCode) =
                await tenantService.CreateTenantAsync(dto, pgId, userId, branchId);

            if (!success)
                return StatusCode(statusCode, result);

            return Ok(result);
        }



        [AccessPoint("Tenant", "Update Tenant")]
        [HttpPut("{tenantId}")]
        public async Task<IActionResult> UpdateTenant(string tenantId, UpdateTenantDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var tenant = await context.Tenants.SingleOrDefaultAsync(t => t.TenantId == tenantId && t.PgId == pgId);
            if (tenant == null)
            {
                var otherPgName = await context.Tenants
                    .Where(t => t.TenantId == tenantId && !t.isDeleted)
                    .Join(context.PGs, t => t.PgId, pg => pg.PgId, (t, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return StatusCode(403, $"This tenant belongs to {otherPgName}. Please login to {otherPgName} to modify it.");
                return NotFound();
            }

            tenant.Name = dto.Name;
            tenant.ContactNumber = dto.ContactNumber;
            tenant.AadharNumber = dto.AadharNumber;
            tenant.Notes = dto.Notes;
            tenant.Email = dto.Email;
            tenant.UpdatedAt = DateTime.Now;

            await context.SaveChangesAsync();

            return NoContent();
        }
        [AccessPoint("Tenant", "Delete Tenant")]
        [HttpDelete("{tenantId}")]
        public async Task<IActionResult> DeleteTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (pgId == null)
                return Unauthorized();

            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.TenantId == tenantId &&
                    t.PgId == pgId &&
                    !t.isDeleted);

            if (tenant == null)
            {
                var otherPgName = await context.Tenants
                    .Where(t => t.TenantId == tenantId && !t.isDeleted)
                    .Join(context.PGs, t => t.PgId, pg => pg.PgId, (t, pg) => pg.Name)
                    .FirstOrDefaultAsync();
                if (otherPgName != null)
                    return StatusCode(403, $"This tenant belongs to {otherPgName}. Please login to {otherPgName} to modify it.");
                return NotFound();
            }

            //  Check active room assignment
            var hasActiveRoom = await context.TenantRooms.AnyAsync(tr =>
                tr.TenantId == tenantId &&
                tr.PgId == pgId &&
                tr.ToDate == null);

            if (hasActiveRoom)
                return Conflict("Active tenant cannot be deleted. Move out first.");

            //  Check active rent linkage
            var hasActiveRent = await context.TenantRentHistories.AnyAsync(trh =>
                trh.TenantId == tenantId &&
                trh.ToDate == null);

            if (hasActiveRent)
                return Conflict("Tenant has active rent configuration. Move out first.");

            // Soft delete
            tenant.isDeleted = true;
            tenant.DeletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }
        [HttpGet("findby-aadhar/{aadhar}")]
        public async Task<IActionResult> GetByAadhar(string aadhar)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var tenant = await context.Tenants
                .Where(t =>
                    t.PgId == pgId &&
                    t.AadharNumber == aadhar &&
                    !t.isDeleted)
                .Select(t => new
                {
                    tenantId = t.TenantId,
                    name = t.Name,
                    contactNumber = t.ContactNumber
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound();

            return Ok(tenant);
        }
        [AccessPoint("Tenant", "Check In Tenant")]
        [HttpPost("{tenantId}/create-stay")]
        public async Task<IActionResult> CreateStay(string tenantId, CreateStayDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var (success, result, statusCode) =
                await tenantService.CreateStayAsync(tenantId, dto, pgId);

            if (!success)
                return StatusCode(statusCode, result);

            return Ok(result);
        }
    }
}
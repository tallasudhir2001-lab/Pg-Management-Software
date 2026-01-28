using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Tenant;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/tenants")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> GetTenants(
     int page = 1,
     int pageSize = 10,
     string? search = null,
     string? status = null,
     string? roomId = null,
     string sortBy = "updated",
     string sortDir = "desc"
 )
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var query = context.Tenants
                .Where(t => t.PgId == pgId && !t.isDeleted);

            //  Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.Name.Contains(search) ||
                    t.ContactNumber.Contains(search));
            }

            // LEFT JOIN with TenantRoom (active assignment)
            var projectedQuery = query.Select(t => new
            {
                Tenant = t,
                ActiveAssignment = context.TenantRooms
                    .Where(tr =>
                        tr.TenantId == t.TenantId &&
                        tr.PgId == pgId &&
                        tr.ToDate == null)
                    .Select(tr => new
                    {
                        tr.RoomId,
                        tr.Room.RoomNumber,
                        tr.FromDate
                    })
                    .FirstOrDefault()
            });

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                status = status.ToLower();
                projectedQuery = status switch
                {
                    "active" => projectedQuery.Where(x => x.ActiveAssignment != null),
                    "movedout" => projectedQuery.Where(x => x.ActiveAssignment == null),
                    _ => projectedQuery
                };
            }

            // Room filter (only applies to active tenants)
            if (!string.IsNullOrEmpty(roomId))
            {
                projectedQuery = projectedQuery
                    .Where(x =>
                        x.ActiveAssignment != null &&
                        x.ActiveAssignment.RoomId == roomId);
            }

            // Total count
            var totalCount = await projectedQuery.CountAsync();

            // Sorting (NULL SAFE)
            projectedQuery = (sortBy.ToLower(), sortDir.ToLower()) switch
            {
                ("updated", "desc") => projectedQuery
                    .OrderByDescending(x => x.Tenant.UpdatedAt ?? x.Tenant.CreatedAt),

                ("updated", "asc") => projectedQuery
                    .OrderBy(x => x.Tenant.UpdatedAt ?? x.Tenant.CreatedAt),

                ("name", "desc") => projectedQuery
                    .OrderByDescending(x => x.Tenant.Name),

                ("room", "asc") => projectedQuery
                    .OrderBy(x => x.ActiveAssignment == null)
                    .ThenBy(x => x.ActiveAssignment!.RoomNumber),

                ("room", "desc") => projectedQuery
                    .OrderBy(x => x.ActiveAssignment == null)
                    .ThenByDescending(x => x.ActiveAssignment!.RoomNumber),

                ("checkedin", "asc") => projectedQuery
                    .OrderBy(x => x.ActiveAssignment == null)
                    .ThenBy(x => x.ActiveAssignment!.FromDate),

                ("checkedin", "desc") => projectedQuery
                    .OrderBy(x => x.ActiveAssignment == null)
                    .ThenByDescending(x => x.ActiveAssignment!.FromDate),

                _ => projectedQuery.OrderBy(x => x.Tenant.Name)
            };

            // Pagination + projection
            var tenants = await projectedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TenantListDto
                {
                    TenantId = x.Tenant.TenantId,
                    Name = x.Tenant.Name,
                    ContactNumber = x.Tenant.ContactNumber,

                    RoomId = x.ActiveAssignment != null ? x.ActiveAssignment.RoomId : null,
                    RoomNumber = x.ActiveAssignment != null ? x.ActiveAssignment.RoomNumber : null,

                    CheckedInAt = x.ActiveAssignment != null
                        ? x.ActiveAssignment.FromDate
                        : null,

                    Status = x.ActiveAssignment == null ? "MovedOut" : "Active"
                })
                .ToListAsync();

            return Ok(new PageResultsDto<TenantListDto>
            {
                Items = tenants,
                TotalCount = totalCount
            });
        }

        [HttpGet("{tenantId}")]
        public async Task<IActionResult> GetTenantById(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var tenant = await context.Tenants
                .Where(t =>
                    t.TenantId == tenantId &&
                    t.PgId == pgId &&
                    !t.isDeleted)
                .Select(t => new
                {
                    t.TenantId,
                    t.Name,
                    t.ContactNumber,
                    t.AadharNumber,
                    t.AdvanceAmount,
                    t.RentPaidUpto,
                    t.Notes,

                    ActiveAssignment = context.TenantRooms
                        .Where(tr =>
                            tr.TenantId == tenantId &&
                            tr.PgId == pgId &&
                            tr.ToDate == null)
                        .Select(tr => new
                        {
                            tr.Room.RoomNumber,
                            tr.FromDate
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
                return NotFound();

            return Ok(new
            {
                tenant.TenantId,
                tenant.Name,
                tenant.ContactNumber,
                tenant.AadharNumber,
                tenant.AdvanceAmount,
                tenant.RentPaidUpto,
                tenant.Notes,

                RoomNumber = tenant.ActiveAssignment?.RoomNumber,
                CheckedInAt = tenant.ActiveAssignment?.FromDate,
                Status = tenant.ActiveAssignment == null ? "MovedOut" : "Active"
            });
        }

        [HttpPost("{tenantId}/change-room")]
        public async Task<IActionResult> ChangeRoom(string tenantId, ChangeRoomDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            // 1️ Get active room assignment
            var activeAssignment = await context.TenantRooms
                .FirstOrDefaultAsync(tr =>
                    tr.TenantId == tenantId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null);

            if (activeAssignment == null)
                return BadRequest("Tenant does not have an active room.");

            if (activeAssignment.RoomId == dto.newRoomId)
                return BadRequest("Tenant is already in this room.");

            // 2️ Validate new room & capacity
            var room = await context.Rooms
                .Where(r => r.RoomId == dto.newRoomId && r.PgId == pgId)
                .Select(r => new
                {
                    r.RoomId,
                    r.Capacity,
                    OccupiedBeds = context.TenantRooms.Count(tr =>
                        tr.RoomId == r.RoomId &&
                        tr.PgId == pgId &&
                        tr.ToDate == null)
                })
                .FirstOrDefaultAsync();

            if (room == null)
                return BadRequest("Invalid room.");

            if (room.OccupiedBeds >= room.Capacity)
                return BadRequest("Room is already full.");

            // 3️ Get current rent for new room
            var newRoomRent = await context.RoomRentHistories
                .FirstOrDefaultAsync(rrh =>
                    rrh.RoomId == room.RoomId &&
                    rrh.EffectiveTo == null);

            if (newRoomRent == null)
                return BadRequest("New room has no active rent configuration.");

            using var tx = await context.Database.BeginTransactionAsync();

            var now = DateTime.UtcNow;

            // 4️ Close old room assignment
            activeAssignment.ToDate = now;

            // 5️ Close active tenant rent history
            var activeRent = await context.TenantRentHistories
                .FirstOrDefaultAsync(trh =>
                    trh.TenantId == tenantId &&
                    trh.ToDate == null);

            if (activeRent != null)
            {
                activeRent.ToDate = now;
            }

            // 6️ Create new TenantRoom
            context.TenantRooms.Add(new TenantRoom
            {
                TenantRoomId = Guid.NewGuid(),
                TenantId = tenantId,
                RoomId = room.RoomId,
                PgId = pgId,
                FromDate = now
            });

            // 7️⃣ Create new TenantRentHistory
            context.TenantRentHistories.Add(new TenantRentHistory
            {
                TenantRentHistoryId = Guid.NewGuid(),
                TenantId = tenantId,
                RoomRentHistoryId = newRoomRent.RoomRentHistoryId,
                FromDate = now,
                ToDate = null
            });

            // 8️⃣ Update tenant metadata
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t =>
                    t.TenantId == tenantId &&
                    t.PgId == pgId &&
                    !t.isDeleted);

            if (tenant != null)
            {
                tenant.UpdatedAt = now;
            }

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return NoContent();
        }

        [HttpPost("{tenantId}/move-out")]
        public async Task<IActionResult> MoveOutTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            using var tx = await context.Database.BeginTransactionAsync();

            // 1️⃣ Close active room assignment
            var activeRoom = await context.TenantRooms
                .FirstOrDefaultAsync(tr =>
                    tr.TenantId == tenantId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null);

            if (activeRoom == null)
                return BadRequest("Tenant does not have an active room.");

            activeRoom.ToDate = DateTime.UtcNow;

            // 2️⃣ Close active rent history
            var activeRent = await context.TenantRentHistories
                .FirstOrDefaultAsync(trh =>
                    trh.TenantId == tenantId &&
                    trh.ToDate == null);

            if (activeRent != null)
            {
                activeRent.ToDate = DateTime.UtcNow;
            }

            // 3️ Update tenant metadata (optional but recommended)
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

        [HttpPost("create-tenant")]
        public async Task<IActionResult> CreateTenant(CreateTenantDto createTenantDto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            //  Validate room & capacity
            var room = await context.Rooms
                .Where(r => r.RoomId == createTenantDto.RoomId && r.PgId == pgId)
                .Select(r => new
                {
                    r.RoomId,
                    r.Capacity,
                    OccupiedBeds = context.TenantRooms.Count(tr =>
                        tr.RoomId == r.RoomId &&
                        tr.PgId == pgId &&
                        tr.ToDate == null
                    )
                })
                .FirstOrDefaultAsync();

            if (room == null)
                return BadRequest("Invalid room.");

            if (room.OccupiedBeds >= room.Capacity)
                return BadRequest("Room is already full.");

            //  Get current active rent
            var currentRent = await context.RoomRentHistories
                .FirstOrDefaultAsync(rrh =>
                    rrh.RoomId == room.RoomId &&
                    rrh.EffectiveTo == null);

            if (currentRent == null)
                return BadRequest("Room has no active rent configuration.");

            using var tx = await context.Database.BeginTransactionAsync();

            // 1 Create Tenant
            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid().ToString(),
                PgId = pgId,
                Name = createTenantDto.Name,
                ContactNumber = createTenantDto.ContactNumber,
                AadharNumber = createTenantDto.AadharNumber,
                AdvanceAmount = createTenantDto.AdvanceAmount,
                RentPaidUpto = createTenantDto.RentPaidUpto,
                Notes = createTenantDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tenants.Add(tenant);

            // 2️ Create TenantRoom
            var fromDate = createTenantDto.FromDate ?? DateTime.UtcNow;

            var tenantRoom = new TenantRoom
            {
                TenantRoomId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                RoomId = room.RoomId,
                PgId = pgId,
                FromDate = fromDate,
                ToDate = createTenantDto.ToDate
            };
            context.TenantRooms.Add(tenantRoom);

            // 3️ Create TenantRentHistory (NEW, REQUIRED)
            var tenantRentHistory = new TenantRentHistory
            {
                TenantRentHistoryId = Guid.NewGuid(),
                TenantId = tenant.TenantId,
                RoomRentHistoryId = currentRent.RoomRentHistoryId,
                FromDate = fromDate,
                ToDate = null
            };
            context.TenantRentHistories.Add(tenantRentHistory);
            try
            {
                await context.SaveChangesAsync();
            }
            catch(DbUpdateException ex)
            {

            }
            await tx.CommitAsync();

            return Ok(new { tenantId = tenant.TenantId });
        }

        [HttpPut("{tenantId}")]
        public async Task<IActionResult> UpdateTenant(string tenantId, UpdateTenantDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var tenant = await context.Tenants.SingleOrDefaultAsync(t => t.TenantId == tenantId && t.PgId == pgId);
            if (tenant == null)
                return NotFound();

            tenant.Name = dto.Name;
            tenant.ContactNumber = dto.ContactNumber;
            tenant.AadharNumber = dto.AadharNumber;
            tenant.AdvanceAmount = dto.AdvanceAmount;
            tenant.RentPaidUpto = dto.RentPaidUpto;
            tenant.Notes = dto.Notes;
            tenant.UpdatedAt = DateTime.Now;

            await context.SaveChangesAsync();

            return NoContent();
        }
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
                return NotFound();

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

    }
}

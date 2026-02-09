using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Room;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/rooms")]
    [ApiController]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        public RoomsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> GetRooms(
    int page = 1,
    int pageSize = 12,
    string? search = null,
    string? status = null,
    string? ac = null,
    string? vacancies = null
)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            IQueryable<Room> query = context.Rooms
                .Where(r => r.PgId == pgId);

            // 🔍 Search by Room Number
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.RoomNumber.Contains(search));
            }

            // ❄️ AC / Non-AC filter (via RoomRentHistory)
            if (!string.IsNullOrWhiteSpace(ac))
            {
                if (ac.Equals("ac", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r =>
                        context.RoomRentHistories.Any(rrh =>
                            rrh.RoomId == r.RoomId &&
                            rrh.EffectiveTo == null &&
                            rrh.IsAc
                        ));
                }
                else if (ac.Equals("non-ac", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r =>
                        context.RoomRentHistories.Any(rrh =>
                            rrh.RoomId == r.RoomId &&
                            rrh.EffectiveTo == null &&
                            !rrh.IsAc
                        ));
                }
            }


            // 🧮 Project with OccupiedBeds + Current Rent
            var projectedQuery = query.Select(r => new
            {
                Room = r,

                OccupiedBeds = context.TenantRooms.Count(tr =>
                    tr.RoomId == r.RoomId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null
                ),

                CurrentRent = context.RoomRentHistories
                    .Where(rrh =>
                        rrh.RoomId == r.RoomId &&
                        rrh.EffectiveTo == null)
                    .Select(rrh => new
                    {
                        rrh.RentAmount,
                        rrh.IsAc
                    })
                    .FirstOrDefault()
            });

            // 📌 Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.ToLower();

                projectedQuery = status switch
                {
                    "available" => projectedQuery.Where(x => x.OccupiedBeds == 0),

                    "full" => projectedQuery.Where(x =>
                        x.OccupiedBeds >= x.Room.Capacity),

                    "partial" => projectedQuery.Where(x =>
                        x.OccupiedBeds > 0 &&
                        x.OccupiedBeds < x.Room.Capacity),

                    _ => projectedQuery
                };
            }
            // 🏠 Vacancies filter
            if (!string.IsNullOrWhiteSpace(vacancies))
            {
                if (vacancies == "0")
                {
                    // 0 vacancies = Full rooms
                    projectedQuery = projectedQuery.Where(x =>
                        x.Room.Capacity - x.OccupiedBeds == 0);
                }
                else if (vacancies == "1")
                {
                    // Exactly 1 vacancy
                    projectedQuery = projectedQuery.Where(x =>
                        x.Room.Capacity - x.OccupiedBeds == 1);
                }
                else if (vacancies == "2")
                {
                    // Exactly 2 vacancies
                    projectedQuery = projectedQuery.Where(x =>
                        x.Room.Capacity - x.OccupiedBeds == 2);
                }
                else if (vacancies == "3+")
                {
                    // 3 or more vacancies
                    projectedQuery = projectedQuery.Where(x =>
                        x.Room.Capacity - x.OccupiedBeds >= 3);
                }
            }

            // 🔢 Total count
            var totalCount = await projectedQuery.CountAsync();

            // 📄 Pagination + final projection
            var rooms = await projectedQuery
                .OrderBy(x => x.Room.RoomNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RoomDto
                {
                    RoomId = x.Room.RoomId,
                    RoomNumber = x.Room.RoomNumber,
                    Capacity = x.Room.Capacity,
                    Occupied = x.OccupiedBeds,
                    Vacancies = x.Room.Capacity - x.OccupiedBeds,

                    RentAmount = x.CurrentRent != null ? x.CurrentRent.RentAmount : 0,
                    isAc = x.CurrentRent != null && x.CurrentRent.IsAc,

                    Status =
                        x.OccupiedBeds == 0 ? "Available" :
                        x.OccupiedBeds >= x.Room.Capacity ? "Full" :
                        "Partial"
                })
                .ToListAsync();

            return Ok(new PageResultsDto<RoomDto>
            {
                Items = rooms,
                TotalCount = totalCount
            });
        }




        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetRoomById(string roomId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var room = await context.Rooms
                .Where(r => r.RoomId == roomId && r.PgId == pgId)
                .Select(r => new
                {
                    r.RoomId,
                    r.RoomNumber,
                    r.Capacity,

                    // current active rent
                    CurrentRent = context.RoomRentHistories
                        .Where(rrh =>
                            rrh.RoomId == r.RoomId &&
                            rrh.EffectiveTo == null)
                        .Select(rrh => new
                        {
                            rrh.RentAmount,
                            rrh.IsAc
                        })
                        .FirstOrDefault(),

                    OccupiedBeds = context.TenantRooms.Count(tr =>
                        tr.RoomId == r.RoomId &&
                        tr.PgId == pgId &&
                        tr.ToDate == null
                    )
                })
                .Select(r => new RoomDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    Capacity = r.Capacity,
                    Occupied = r.OccupiedBeds,
                    Vacancies = r.Capacity - r.OccupiedBeds,

                    RentAmount = r.CurrentRent != null ? r.CurrentRent.RentAmount : 0,
                    isAc = r.CurrentRent != null && r.CurrentRent.IsAc,

                    Status =
                        r.OccupiedBeds == 0 ? "Available" :
                        r.OccupiedBeds >= r.Capacity ? "Full" :
                        "Partial"
                })
                .FirstOrDefaultAsync();

            if (room == null)
                return NotFound();

            return Ok(room);
        }


        [HttpPost("add-room")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var roomExists = await context.Rooms
                .AnyAsync(r => r.PgId == pgId && r.RoomNumber == dto.RoomNumber);

            if (roomExists)
                return Conflict("Room number already exists in this PG.");

            using var transaction = await context.Database.BeginTransactionAsync();

            // 1️⃣ Create Room (NO rent here)
            var room = new Room
            {
                RoomId = Guid.NewGuid().ToString(),
                PgId = pgId,
                RoomNumber = dto.RoomNumber,
                Capacity = dto.Capacity
            };

            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            // 2️⃣ Create initial rent history
            var roomRentHistory = new RoomRentHistory
            {
                RoomId = room.RoomId,
                RentAmount = dto.RentAmount,
                IsAc = dto.IsAc,
                EffectiveFrom = DateTime.Now,
                EffectiveTo = null
            };

            context.RoomRentHistories.Add(roomRentHistory);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return CreatedAtAction(nameof(GetRooms), new { }, null);
        }

        [HttpPut("{roomId}")]
        public async Task<IActionResult> UpdateRoom(string roomId, [FromBody] UpdateRoomDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            using var transaction = await context.Database.BeginTransactionAsync();

            var room = await context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.PgId == pgId);

            if (room == null)
                return NotFound("Room not found.");

            var duplicateRoom = await context.Rooms.AnyAsync(r =>
                r.PgId == pgId &&
                r.RoomNumber == dto.RoomNumber &&
                r.RoomId != roomId);

            if (duplicateRoom)
                return Conflict("Another room with same number already exists.");

            // 1️⃣ Update non-financial fields
            room.RoomNumber = dto.RoomNumber;
            room.Capacity = dto.Capacity;

            // 2️⃣ Fetch current active rent
            var currentRent = await context.RoomRentHistories
                .FirstOrDefaultAsync(rr =>
                    rr.RoomId == roomId &&
                    rr.EffectiveTo == null);

            if (currentRent == null)
                return BadRequest("Room has no active rent configuration.");

            var isRentChanged =
                currentRent.RentAmount != dto.RentAmount ||
                currentRent.IsAc != dto.isAc;

            // 3️⃣ Handle rent/AC change
            if (isRentChanged)
            {
                // Close current rent
                currentRent.EffectiveTo = DateTime.Now;

                // Create new rent record
                var newRent = new RoomRentHistory
                {
                    RoomId = roomId,
                    RentAmount = dto.RentAmount,
                    IsAc = dto.isAc,
                    EffectiveFrom = DateTime.Now
                };

                context.RoomRentHistories.Add(newRent);
                await context.SaveChangesAsync();

                // Find active tenant rent histories for this room
                var activeTenantRents = await context.TenantRentHistories
                    .Include(trh => trh.RoomRentHistory)
                    .Where(trh =>
                        trh.ToDate == null &&
                        trh.RoomRentHistory.RoomId == roomId)
                    .ToListAsync();

                // Close old tenant rent history and create new ones
                foreach (var tenantRent in activeTenantRents)
                {
                    tenantRent.ToDate = DateTime.Now;

                    context.TenantRentHistories.Add(new TenantRentHistory
                    {
                        TenantId = tenantRent.TenantId,
                        RoomRentHistoryId = newRent.RoomRentHistoryId,
                        FromDate = DateTime.Now
                    });
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }

        [HttpDelete("{roomId}")]
        public async Task<IActionResult> DeleteRoom(string roomId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var room = await context.Rooms
                .Include(r => r.Tenants)
                .FirstOrDefaultAsync(r => r.RoomId == roomId && r.PgId == pgId);

            if (room == null)
                return NotFound();

            // 1. Check ANY tenant-room history (active OR moved-out)
            var hasAnyTenantHistory = await context.TenantRooms.AnyAsync(tr =>
                tr.RoomId == roomId &&
                tr.PgId == pgId
            );

            if (hasAnyTenantHistory)
                return BadRequest("Room cannot be deleted because tenants are or were assigned to it.");

            using var tx = await context.Database.BeginTransactionAsync();

            // 2. Delete room rent history
            var rentHistories = await context.RoomRentHistories
                .Where(rrh => rrh.RoomId == roomId)
                .ToListAsync();

            context.RoomRentHistories.RemoveRange(rentHistories);

            // 3. Delete room
            context.Rooms.Remove(room);

            await context.SaveChangesAsync();
            await tx.CommitAsync();

            return NoContent();
        }

        [HttpGet("{roomId}/tenants")]
        public async Task<IActionResult> GetTenantsInRoom(string roomId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var tenants = await context.TenantRooms
                .AsNoTracking()
                .Where(tr =>
                    tr.RoomId == roomId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null)
                .Select(tr => new
                {
                    tr.Tenant.TenantId,
                    tr.Tenant.Name,
                    tr.Tenant.ContactNumber,
                    CheckedInAt = tr.FromDate,
                    Status = "Active"
                })
                .ToListAsync();

            return Ok(tenants);
        }

    }
}

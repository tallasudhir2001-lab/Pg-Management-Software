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
    string? ac = null
)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            IQueryable<Room> query = context.Rooms
                .Where(r => r.PgId == pgId);

            //  Search by Room Number
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.RoomNumber.Contains(search));
            }

            //  AC / Non-AC filter
            if (!string.IsNullOrWhiteSpace(ac))
            {
                if (ac.Equals("ac", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => r.isAc);
                else if (ac.Equals("non-ac", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(r => !r.isAc);
            }

            //  Project once with OccupiedBeds (FROM TenantRoom)
            var projectedQuery = query.Select(r => new
            {
                Room = r,
                OccupiedBeds = context.TenantRooms.Count(tr =>
                    tr.RoomId == r.RoomId &&
                    tr.PgId == pgId &&
                    tr.ToDate == null
                )
            });

            //  Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.ToLower();

                projectedQuery = status switch
                {
                    "available" => projectedQuery.Where(x => x.OccupiedBeds == 0),

                    "full" => projectedQuery.Where(x => x.OccupiedBeds >= x.Room.Capacity),

                    "partial" => projectedQuery.Where(x =>
                        x.OccupiedBeds > 0 &&
                        x.OccupiedBeds < x.Room.Capacity
                    ),

                    _ => projectedQuery
                };
            }

            // 🔢 Total count (after filters)
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
                    RentAmount = x.Room.RentAmount,
                    isAc = x.Room.isAc,
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
                    r.RentAmount,
                    r.isAc,

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
                    RentAmount = r.RentAmount,
                    Status =
                        r.OccupiedBeds == 0 ? "Available" :
                        r.OccupiedBeds >= r.Capacity ? "Full" :
                        "Partial",
                    isAc = r.isAc
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
            if(string.IsNullOrEmpty(pgId))
                return Unauthorized("gg");

            var roomExists = await context.Rooms
        .AnyAsync(r => r.PgId == pgId && r.RoomNumber == dto.RoomNumber);

            if (roomExists)
                return Conflict("Room number already exists in this PG.");

            var room = new Room
            {
                RoomId = Guid.NewGuid().ToString(),
                PgId = pgId,
                RoomNumber = dto.RoomNumber,
                Capacity = dto.Capacity,
                RentAmount = dto.RentAmount,
                isAc=dto.IsAc
            };
            context.Rooms.Add(room);
            await context.SaveChangesAsync();

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

            room.RoomNumber = dto.RoomNumber;
            room.Capacity = dto.Capacity;
            room.RentAmount = dto.RentAmount;
            room.isAc = dto.isAc;

            await context.SaveChangesAsync();

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

            var hasActiveAssignments = await context.TenantRooms.AnyAsync(tr =>
                tr.RoomId == roomId &&
                tr.PgId == pgId &&
                tr.ToDate == null
            );

            if (hasActiveAssignments)
                return BadRequest("Cannot delete room with active tenants.");

            context.Rooms.Remove(room);
            await context.SaveChangesAsync();

            return NoContent();
        }

    }
}

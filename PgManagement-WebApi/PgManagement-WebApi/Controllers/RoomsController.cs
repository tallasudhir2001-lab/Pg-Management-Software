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

            // Base query (tenant-safe)
            IQueryable<Room> query = context.Rooms
                .Where(r => r.PgId == pgId);

            // 🔍 Search by Room Number
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.RoomNumber.Contains(search));
            }

            // ❄️ AC / Non-AC filter
            if (!string.IsNullOrWhiteSpace(ac))
            {
                if (ac.Equals("ac", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => r.isAc);
                }
                else if (ac.Equals("non-ac", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => !r.isAc);
                }
            }

            // 📊 Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                status = status.ToLower();

                query = status switch
                {
                    "available" => query.Where(r =>
                        !r.Tenants.Any(t => t.isActive)
                    ),

                    "full" => query.Where(r =>
                        r.Tenants.Count(t => t.isActive) >= r.Capacity
                    ),

                    "partial" => query.Where(r =>
                        r.Tenants.Any(t => t.isActive) &&
                        r.Tenants.Count(t => t.isActive) < r.Capacity
                    ),

                    _ => query
                };
            }

            // 🔢 Total count (AFTER filters, BEFORE pagination)
            var totalCount = await query.CountAsync();

            // 📄 Apply pagination + projection
            var rooms = await query
                .OrderBy(r => r.RoomNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RoomDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    Capacity = r.Capacity,
                    Occupied = r.Tenants.Count(t => t.isActive),
                    Vacancies = r.Capacity - r.Tenants.Count(t => t.isActive),
                    RentAmount = r.RentAmount,
                    isAc = r.isAc,
                    Status =
                        r.Tenants.Count(t => t.isActive) == 0 ? "Available" :
                        r.Tenants.Count(t => t.isActive) >= r.Capacity ? "Full" :
                        "Partial"
                })
                .ToListAsync();

            // 📦 Final response
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
                .Select(r => new RoomDto
                {
                    RoomId = r.RoomId,
                    RoomNumber = r.RoomNumber,
                    Capacity = r.Capacity,
                    Occupied = r.Tenants.Count(t => t.isActive),
                    Vacancies = r.Capacity - r.Tenants.Count(t => t.isActive),
                    RentAmount = r.RentAmount,
                    Status =
                        r.Tenants.Count(t => t.isActive) == 0 ? "Available" :
                        r.Tenants.Count(t => t.isActive) >= r.Capacity ? "Full" :
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
                RentAmount = dto.RentAmount
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

            if (room.Tenants.Any(t => t.isActive))
                return BadRequest("Cannot delete room with active tenants.");

            context.Rooms.Remove(room);
            await context.SaveChangesAsync();

            return NoContent();
        }

    }
}

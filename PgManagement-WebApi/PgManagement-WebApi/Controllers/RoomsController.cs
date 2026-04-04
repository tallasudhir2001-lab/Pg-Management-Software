using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Room;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/rooms")]
    [ApiController]
    [Authorize]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly ApplicationDbContext _context;

        public RoomsController(IRoomService roomService, ApplicationDbContext context)
        {
            _roomService = roomService;
            _context = context;
        }

        [AccessPoint("Room", "View All Rooms")]
        [HttpGet]
        public async Task<IActionResult> GetRooms(
            int page = 1, int pageSize = 12,
            string? search = null, string? status = null,
            string? ac = null, string? vacancies = null)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _roomService.GetRoomsAsync(pgIds, page, pageSize, search, status, ac, vacancies));
        }

        [AccessPoint("Room", "View Room Details")]
        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetRoomById(string roomId)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            var room = await _roomService.GetRoomByIdAsync(roomId, pgIds);
            if (room == null) return NotFound();
            return Ok(room);
        }

        [AccessPoint("Room", "Create Room")]
        [HttpPost("add-room")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _roomService.CreateRoomAsync(pgId, branchId, dto);
            if (!success) return StatusCode(statusCode, result);
            return CreatedAtAction(nameof(GetRooms), new { }, null);
        }

        [AccessPoint("Room", "Update Room")]
        [HttpPut("{roomId}")]
        public async Task<IActionResult> UpdateRoom(string roomId, [FromBody] UpdateRoomDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
            var (success, result, statusCode) = await _roomService.UpdateRoomAsync(roomId, pgId, dto, userId);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("Room", "Delete Room")]
        [HttpDelete("{roomId}")]
        public async Task<IActionResult> DeleteRoom(string roomId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _roomService.DeleteRoomAsync(roomId, pgId);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [HttpGet("{roomId}/tenants")]
        public async Task<IActionResult> GetTenantsInRoom(string roomId)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _roomService.GetTenantsInRoomAsync(roomId, pgIds));
        }
    }
}

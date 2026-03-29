using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.DTOs.Booking;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [AccessPoint("Booking", "View All Bookings")]
        [HttpGet]
        public async Task<IActionResult> GetBookings([FromQuery] BookingListQueryDto query)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var result = await _bookingService.GetBookingsAsync(pgId, query);
            return Ok(result);
        }

        [AccessPoint("Booking", "View Booking Details")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var booking = await _bookingService.GetBookingByIdAsync(pgId, id);
            if (booking == null) return NotFound();
            return Ok(booking);
        }

        [HttpGet("check-active/{tenantId}")]
        public async Task<IActionResult> CheckActiveBooking(string tenantId)
        {
            var hasActive = await _bookingService.HasActiveBookingAsync(tenantId);
            return Ok(new { hasActiveBooking = hasActive });
        }

        [AccessPoint("Booking", "Create Booking")]
        [HttpPost("create-booking")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            var id = await _bookingService.CreateBookingAsync(pgId, dto, userId, branchId);
            return CreatedAtAction(nameof(GetById), new { id }, null);
        }

        [AccessPoint("Booking", "Update Booking")]
        [HttpPut("update-booking/{id}")]
        public async Task<IActionResult> UpdateBooking(string id, [FromBody] UpdateBookingDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            await _bookingService.UpdateBookingAsync(pgId, id, dto);
            return NoContent();
        }

        [AccessPoint("Booking", "Cancel Booking")]
        [HttpPatch("cancel-booking/{id}")]
        public async Task<IActionResult> CancelBooking(string id)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            await _bookingService.CancelBookingAsync(pgId, id);
            return NoContent();
        }

        [AccessPoint("Booking", "Terminate Booking")]
        [HttpPatch("terminate-booking/{id}")]
        public async Task<IActionResult> TerminateBooking(string id)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            await _bookingService.TerminateBookingAsync(pgId, id);
            return NoContent();
        }

        [AccessPoint("Booking", "Terminate No-Show Bookings")]
        [HttpPost("terminate-no-shows")]
        public async Task<IActionResult> TerminateNoShows()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId))
                return Unauthorized();

            await _bookingService.TerminateNoShowBookingsAsync(pgId);
            return NoContent();
        }
    }
}
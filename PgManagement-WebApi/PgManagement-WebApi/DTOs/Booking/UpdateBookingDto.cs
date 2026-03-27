using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Booking
{
    public class UpdateBookingDto
    {
        [Required]
        public string RoomId { get; set; }

        [Required]
        public DateTime ScheduledCheckInDate { get; set; }

        public decimal AdvanceAmount { get; set; }

        public string? Notes { get; set; }
    }
}
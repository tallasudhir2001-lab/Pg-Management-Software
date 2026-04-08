using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Booking
{
    public class CreateBookingDto
    {
        // Tenant details — used to find or create tenant
        [Required]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar must be 12 digits")]
        public string AadharNumber { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
        public string ContactNumber { get; set; }

        // Booking details
        [Required]
        public string RoomId { get; set; }

        [Required]
        public DateTime ScheduledCheckInDate { get; set; }

        public decimal? AdvanceAmount { get; set; }

        // Required only when AdvanceAmount > 0
        public string? PaymentModeCode { get; set; }

        public string? Notes { get; set; }
    }
}
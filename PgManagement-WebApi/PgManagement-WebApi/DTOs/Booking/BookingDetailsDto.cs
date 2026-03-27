namespace PgManagement_WebApi.DTOs.Booking
{
    public class BookingDetailsDto
    {
        public string BookingId { get; set; }
        public string TenantId { get; set; }
        public string TenantName { get; set; }
        public string TenantContact { get; set; }
        public string RoomId { get; set; }
        public string RoomNumber { get; set; }
        public DateTime ScheduledCheckInDate { get; set; }
        public string Status { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
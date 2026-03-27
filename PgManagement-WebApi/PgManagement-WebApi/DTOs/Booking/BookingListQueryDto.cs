namespace PgManagement_WebApi.DTOs.Booking
{
    public class BookingListQueryDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Status { get; set; }
        public string? RoomId { get; set; }

        public string? SortBy { get; set; }
        public string SortDir { get; set; } = "desc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

namespace PgManagement_WebApi.DTOs.Tenant
{
    public class TenantRoomDto
    {
        public string TenantId { get; set; } = default!;
        public string RoomId { get; set; } = default!;
        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string RoomNumber { get; set; } = default!;
        public string StayType { get; set; } = "MONTHLY";
    }
}

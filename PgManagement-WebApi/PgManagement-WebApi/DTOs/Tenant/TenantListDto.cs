namespace PgManagement_WebApi.DTOs.Tenant
{
    public class TenantListDto
    {
        public string TenantId { get; set; }
        public string Name { get; set; }

        public string? RoomId { get; set; }
        public string? RoomNumber { get; set; }

        public string ContactNumber { get; set; }

        public string Status { get; set; }   // Active / MovedOut
        public DateTime? CheckedInAt { get; set; }
        public bool IsRentPending { get; set; }

    }
}

namespace PgManagement_WebApi.DTOs.Tenant
{
    public class CreateStayDto
    {
        public string RoomId { get; set; } = default!;
        public DateTime? FromDate { get; set; }
        public decimal? AdvanceAmount { get; set; }
    }
}

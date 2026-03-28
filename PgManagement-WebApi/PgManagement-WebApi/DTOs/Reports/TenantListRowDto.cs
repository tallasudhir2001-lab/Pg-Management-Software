namespace PgManagement_WebApi.DTOs.Reports
{
    public class TenantListRowDto
    {
        public string TenantName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string AadhaarMasked { get; set; } = "";
        public string? RoomNumber { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public string Status { get; set; } = "";
        public decimal MonthlyRent { get; set; }
    }

    public class TenantListReportDto
    {
        public string StatusFilter { get; set; } = "Active";
        public List<TenantListRowDto> Rows { get; set; } = new();
    }
}

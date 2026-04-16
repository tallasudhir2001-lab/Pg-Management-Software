namespace PgManagement_WebApi.DTOs.Reports
{
    public class RoomChangeRowDto
    {
        public string TenantName { get; set; } = "";
        public DateTime ChangeDate { get; set; }
        public string OldRoom { get; set; } = "";
        public string NewRoom { get; set; } = "";
        public decimal OldRent { get; set; }
        public decimal NewRent { get; set; }
    }

    public class RoomChangeHistoryReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<RoomChangeRowDto> Rows { get; set; } = new();
        public int TotalChanges { get; set; }
    }
}

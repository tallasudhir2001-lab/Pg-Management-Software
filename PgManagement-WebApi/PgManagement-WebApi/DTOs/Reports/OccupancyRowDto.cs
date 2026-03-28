namespace PgManagement_WebApi.DTOs.Reports
{
    public class OccupancyRowDto
    {
        public string RoomNumber { get; set; } = "";
        public int TotalBeds { get; set; }
        public int OccupiedBeds { get; set; }
        public int VacantBeds { get; set; }
        public double OccupancyPercent { get; set; }
        public string TenantNames { get; set; } = "";
    }

    public class OccupancyReportDto
    {
        public DateTime AsOfDate { get; set; }
        public List<OccupancyRowDto> Rows { get; set; } = new();
        public int TotalRooms { get; set; }
        public int TotalBeds { get; set; }
        public int TotalOccupied { get; set; }
        public int TotalVacant { get; set; }
        public double OverallOccupancyPercent { get; set; }
    }
}

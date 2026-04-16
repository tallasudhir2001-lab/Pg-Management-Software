namespace PgManagement_WebApi.DTOs.Reports
{
    public class TurnoverRowDto
    {
        public string TenantName { get; set; } = "";
        public string RoomNumber { get; set; } = "";
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int StayDays { get; set; }
    }

    public class TenantTurnoverReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int MoveIns { get; set; }
        public int MoveOuts { get; set; }
        public double AverageStayDays { get; set; }
        public double ChurnRatePercent { get; set; }
        public List<TurnoverRowDto> MoveOutDetails { get; set; } = new();
    }
}

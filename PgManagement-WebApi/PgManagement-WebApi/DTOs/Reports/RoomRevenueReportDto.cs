namespace PgManagement_WebApi.DTOs.Reports
{
    public class RoomRevenueRowDto
    {
        public string RoomNumber { get; set; } = "";
        public int Capacity { get; set; }
        public int OccupiedDays { get; set; }
        public int VacantDays { get; set; }
        public decimal RentCollected { get; set; }
        public decimal ExpenseAllocated { get; set; }
        public decimal NetRevenue { get; set; }
    }

    public class RoomRevenueReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<RoomRevenueRowDto> Rows { get; set; } = new();
        public decimal TotalRentCollected { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalNetRevenue { get; set; }
    }
}

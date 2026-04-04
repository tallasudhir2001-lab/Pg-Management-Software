namespace PgManagement_WebApi.DTOs.Dashboard
{
    public class DashboardSummaryDto
    {
        public int TotalRooms { get; set; }
        public int TotalTenants { get; set; }
        public int ActiveTenants { get; set; }
        public int MovedOutTenants { get; set; }
        public int OccupiedBeds { get; set; }
        public int VacantBeds { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

    public class RevenueTrendDto
    {
        public int Month { get; set; }   // 1–12
        public decimal Amount { get; set; }
    }

    public class RecentPaymentDto
    {
        public string TenantName { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Mode { get; set; } = null!;
    }
    public class DashboardExpenseSummaryDto
    {
        public decimal TotalExpenses { get; set; }
    }

    public class DashboardAlertsDto
    {
        public int MovedOutWithPendingRent { get; set; }
        public int MovedOutWithUnsettledAdvance { get; set; }
        public int ActiveWithPendingRent { get; set; }
        public int OverdueExpectedCheckouts { get; set; }
        public int OverdueBookings { get; set; }
    }

    public class CollectionSummaryDto
    {
        public decimal ExpectedRent { get; set; }
        public decimal CollectedRent { get; set; }
        public decimal PendingRent { get; set; }
        public double CollectionRate { get; set; }
        public int PaidCount { get; set; }
        public int PendingCount { get; set; }
    }

    public class TodaySnapshotDto
    {
        public decimal TodayCollection { get; set; }
        public decimal TodayExpenses { get; set; }
    }

    public class VacancyLossDto
    {
        public decimal TotalMonthlyLoss { get; set; }
        public int TotalVacantBeds { get; set; }
        public int TotalBeds { get; set; }
        public List<VacancyLossRoomDto> Rooms { get; set; } = new();
    }

    public class VacancyLossRoomDto
    {
        public string RoomId { get; set; } = null!;
        public string RoomNumber { get; set; } = null!;
        public int Capacity { get; set; }
        public int Occupied { get; set; }
        public int VacantBeds { get; set; }
        public decimal RentPerBed { get; set; }
        public decimal MonthlyLoss { get; set; }
    }
}

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
}

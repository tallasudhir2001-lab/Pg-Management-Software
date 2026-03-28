namespace PgManagement_WebApi.DTOs.Reports
{
    public class OverdueRentRowDto
    {
        public string RoomNumber { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string TenantPhone { get; set; } = "";
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? PaidUpTo { get; set; }
        public DateTime OverdueSince { get; set; }
        public int DaysOverdue { get; set; }
        public decimal OutstandingAmount { get; set; }
    }

    public class OverdueRentReportDto
    {
        public DateTime AsOfDate { get; set; }
        public List<OverdueRentRowDto> Rows { get; set; } = new();
        public int TotalOverdueTenants { get; set; }
        public decimal TotalOutstanding { get; set; }
    }
}

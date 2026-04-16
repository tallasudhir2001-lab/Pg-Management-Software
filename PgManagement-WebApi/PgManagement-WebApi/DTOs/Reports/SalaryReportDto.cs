namespace PgManagement_WebApi.DTOs.Reports
{
    public class SalaryReportRowDto
    {
        public string EmployeeName { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime PaymentDate { get; set; }
        public string ForMonth { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = "";
    }

    public class SalaryReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<SalaryReportRowDto> Rows { get; set; } = new();
        public decimal TotalPaid { get; set; }
        public int EmployeeCount { get; set; }
    }
}

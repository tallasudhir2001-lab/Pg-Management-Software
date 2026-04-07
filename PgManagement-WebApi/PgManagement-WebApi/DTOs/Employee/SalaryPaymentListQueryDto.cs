namespace PgManagement_WebApi.DTOs.Employee
{
    public class SalaryPaymentListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? EmployeeId { get; set; }
        public string? ForMonth { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SortBy { get; set; } = "PaymentDate";
        public string? SortDir { get; set; } = "desc";
    }
}

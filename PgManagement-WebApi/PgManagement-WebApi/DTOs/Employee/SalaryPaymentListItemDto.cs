namespace PgManagement_WebApi.DTOs.Employee
{
    public class SalaryPaymentListItemDto
    {
        public string SalaryPaymentId { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ForMonth { get; set; }
        public string PaymentModeCode { get; set; }
        public string PaymentModeLabel { get; set; }
        public string? Notes { get; set; }
        public string? PaidBy { get; set; }
    }
}

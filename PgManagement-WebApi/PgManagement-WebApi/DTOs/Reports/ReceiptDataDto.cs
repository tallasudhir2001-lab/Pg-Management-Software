namespace PgManagement_WebApi.DTOs.Reports
{
    public class ReceiptDataDto
    {
        public string PaymentId { get; set; } = "";
        public string ReceiptNumber { get; set; } = "";
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public DateTime PaidFrom { get; set; }
        public DateTime PaidUpto { get; set; }
        public string? Notes { get; set; }

        public string TenantName { get; set; } = "";
        public string TenantPhone { get; set; } = "";
        public string? TenantEmail { get; set; }
        public string? RoomNumber { get; set; }
    }
}

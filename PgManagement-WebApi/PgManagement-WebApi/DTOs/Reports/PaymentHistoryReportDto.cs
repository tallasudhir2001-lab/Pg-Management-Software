namespace PgManagement_WebApi.DTOs.Reports
{
    public class PaymentHistoryReportRowDto
    {
        public DateTime PaymentDate { get; set; }
        public string ReceiptNumber { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string? RoomNumber { get; set; }
        public string PaymentType { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    public class PaymentHistoryReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<PaymentHistoryReportRowDto> Rows { get; set; } = new();
        public decimal TotalAmount { get; set; }
    }

    public class SendReportDto
    {
        public string ReportType { get; set; } = "";
        public string RecipientEmail { get; set; } = "";
        public string? RecipientName { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }

    public class SendReportWhatsAppDto
    {
        public string ReportType { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string? RecipientName { get; set; }
        public Dictionary<string, string>? Filters { get; set; }
    }
}

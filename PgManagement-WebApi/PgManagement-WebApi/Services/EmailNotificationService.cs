namespace PgManagement_WebApi.Services
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IEmailProvider _emailProvider;
        private readonly IReportService _reportService;

        public EmailNotificationService(IEmailProvider emailProvider, IReportService reportService)
        {
            _emailProvider = emailProvider;
            _reportService = reportService;
        }

        public async Task SendPaymentReceiptAsync(string paymentId, string pgId, string recipientEmail)
        {
            var receiptData = await ((ReportService)_reportService).BuildReceiptDataAsync(paymentId, pgId);
            var pdfBytes = await _reportService.GenerateReceiptAsync(paymentId, pgId);

            var subject = $"Payment Receipt — {receiptData.ReceiptNumber}";
            var html = $"""
                <p>Dear {receiptData.TenantName},</p>
                <p>Please find attached your payment receipt <strong>{receiptData.ReceiptNumber}</strong>
                   dated <strong>{receiptData.PaymentDate:dd MMM yyyy}</strong>
                   for an amount of <strong>₹ {receiptData.Amount:N2}</strong>.</p>
                <p>Period covered: {receiptData.PaidFrom:dd MMM yyyy} — {receiptData.PaidUpto:dd MMM yyyy}</p>
                <br/>
                <p>Thank you.</p>
                """;

            await _emailProvider.SendEmailAsync(
                recipientEmail,
                subject,
                html,
                pdfBytes,
                $"{receiptData.ReceiptNumber}.pdf");
        }

        public async Task SendReportAsync(string reportTitle, byte[] pdfBytes, string recipientEmail)
        {
            var subject = $"Report: {reportTitle}";
            var html = $"""
                <p>Please find attached the report: <strong>{reportTitle}</strong>.</p>
                <p>Generated on {DateTime.Now:dd MMM yyyy HH:mm}.</p>
                """;

            await _emailProvider.SendEmailAsync(
                recipientEmail,
                subject,
                html,
                pdfBytes,
                $"{reportTitle.Replace(" ", "_")}.pdf");
        }
    }
}

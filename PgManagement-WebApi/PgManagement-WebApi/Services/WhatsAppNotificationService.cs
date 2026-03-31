namespace PgManagement_WebApi.Services
{
    public class WhatsAppNotificationService : IWhatsAppNotificationService
    {
        private readonly IWhatsAppProvider _whatsAppProvider;
        private readonly IReportService _reportService;

        public WhatsAppNotificationService(IWhatsAppProvider whatsAppProvider, IReportService reportService)
        {
            _whatsAppProvider = whatsAppProvider;
            _reportService = reportService;
        }

        public async Task SendPaymentReceiptAsync(string paymentId, string pgId, string phoneNumber)
        {
            var receiptData = await ((ReportService)_reportService).BuildReceiptDataAsync(paymentId, pgId);
            var pdfBytes = await _reportService.GenerateReceiptAsync(paymentId, pgId);

            var caption = $"Payment Receipt {receiptData.ReceiptNumber}\n"
                        + $"Amount: ₹{receiptData.Amount:N2}\n"
                        + $"Date: {receiptData.PaymentDate:dd MMM yyyy}\n"
                        + $"Period: {receiptData.PaidFrom:dd MMM yyyy} — {receiptData.PaidUpto:dd MMM yyyy}";

            await _whatsAppProvider.SendDocumentAsync(
                phoneNumber,
                pdfBytes,
                $"{receiptData.ReceiptNumber}.pdf",
                caption);
        }
    }
}

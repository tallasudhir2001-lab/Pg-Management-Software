namespace PgManagement_WebApi.Services
{
    public interface IEmailNotificationService
    {
        Task SendPaymentReceiptAsync(string paymentId, string pgId, string recipientEmail);
        Task SendReportAsync(string reportTitle, byte[] pdfBytes, string recipientEmail);
    }
}

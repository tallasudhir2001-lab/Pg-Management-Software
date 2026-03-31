namespace PgManagement_WebApi.Services
{
    public interface IWhatsAppNotificationService
    {
        Task SendPaymentReceiptAsync(string paymentId, string pgId, string phoneNumber);
    }
}

namespace PgManagement_WebApi.DTOs.Settings
{
    public class NotificationSettingsDto
    {
        public bool AutoSendPaymentReceipt { get; set; }
        public bool SendViaEmail { get; set; }
        public bool SendViaWhatsapp { get; set; }
    }

    public class NotificationSettingsResponseDto
    {
        public bool AutoSendPaymentReceipt { get; set; }
        public bool SendViaEmail { get; set; }
        public bool SendViaWhatsapp { get; set; }

        // Subscription status from PG entity (read-only for owner)
        public bool IsEmailSubscriptionEnabled { get; set; }
        public bool IsWhatsappSubscriptionEnabled { get; set; }
    }
}

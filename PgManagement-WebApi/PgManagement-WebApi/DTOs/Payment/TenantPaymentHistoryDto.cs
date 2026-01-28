namespace PgManagement_WebApi.DTOs.Payment
{
    public class TenantPaymentHistoryDto
    {
        public string PaymentId { get; set; }

        public DateTime PaymentDate { get; set; }

        public DateTime PaidFrom { get; set; }
        public DateTime PaidUpto { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMode { get; set; }   // "UPI", "CASH"
        public string Frequency { get; set; }     // "Monthly", "Daily", "Custom"

        public string CollectedBy { get; set; }   // username / email
    }

}

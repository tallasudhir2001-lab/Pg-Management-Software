namespace PgManagement_WebApi.DTOs.Payment
{
    public class UpdatePaymentDto
    {
        public DateTime PaidFrom { get; set; }
        public DateTime PaidUpto { get; set; }

        public decimal Amount { get; set; }

        public string PaymentModeCode { get; set; } = string.Empty;

        public string PaymentFrequencyCode { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }
}

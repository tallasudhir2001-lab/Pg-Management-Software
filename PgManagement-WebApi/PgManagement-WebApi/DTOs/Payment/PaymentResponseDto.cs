namespace PgManagement_WebApi.DTOs.Payment
{
    public class PaymentResponseDto
    {
        public string PaymentId { get; set; }
        public DateTime PaidFrom { get; set; }
        public DateTime PaidUpto { get; set; }
        public decimal Amount { get; set; }
    }
}

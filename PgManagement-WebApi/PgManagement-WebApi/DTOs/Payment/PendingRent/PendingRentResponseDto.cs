namespace PgManagement_WebApi.DTOs.Payment.PendingRent
{
    public class LastPaymentDto
    {
        public DateTime PaymentDate { get; set; }
        public DateTime PaidFrom { get; set; }
        public DateTime PaidUpto { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = "";
    }

    public class PendingRentResponseDto
    {
        public string TenantId { get; set; }
        public DateTime AsOfDate { get; set; }
        public decimal TotalPendingAmount { get; set; }

        public List<PendingRentBreakdownDto> Breakdown { get; set; }
        public LastPaymentDto? LastPayment { get; set; }
    }
}

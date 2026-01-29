namespace PgManagement_WebApi.DTOs.Payment
{
    public class PaymentHistoryDto
    {
        public string PaymentId { get; set; } = default!;
        public DateTime PaymentDate { get; set; }
        public string TenantName { get; set; } = default!;
        public string PeriodCovered { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Mode { get; set; } = default!;
        public string CollectedBy { get; set; } = default!;
    }

}

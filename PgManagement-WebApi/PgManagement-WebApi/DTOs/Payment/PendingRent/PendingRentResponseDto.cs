namespace PgManagement_WebApi.DTOs.Payment.PendingRent
{
    public class PendingRentResponseDto
    {
        public string TenantId { get; set; }
        public DateTime AsOfDate { get; set; }
        public decimal TotalPendingAmount { get; set; }

        public List<PendingRentBreakdownDto> Breakdown { get; set; }
    }
}

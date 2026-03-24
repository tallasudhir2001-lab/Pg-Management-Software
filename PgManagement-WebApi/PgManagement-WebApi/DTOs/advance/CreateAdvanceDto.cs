namespace PgManagement_WebApi.DTOs.advance
{
    public class CreateAdvanceDto
    {
        public string TenantId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentModeCode { get; set; }
        public string? Notes { get; set; }
    }
}

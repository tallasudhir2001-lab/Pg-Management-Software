namespace PgManagement_WebApi.DTOs.advance
{
    public class SettleAdvanceDto
    {
        public decimal DeductedAmount { get; set; }
        public string PaymentModeCode { get; set; } // refund mode
        public string? Notes { get; set; }
    }
}

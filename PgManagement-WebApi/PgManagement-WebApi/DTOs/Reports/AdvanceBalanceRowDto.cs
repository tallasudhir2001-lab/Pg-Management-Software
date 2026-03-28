namespace PgManagement_WebApi.DTOs.Reports
{
    public class AdvanceBalanceRowDto
    {
        public string TenantName { get; set; } = "";
        public string? RoomNumber { get; set; }
        public decimal AdvancePaid { get; set; }
        public decimal AdvanceRefunded { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = ""; // Held / Fully Refunded / Partially Refunded
    }

    public class AdvanceBalanceReportDto
    {
        public List<AdvanceBalanceRowDto> Rows { get; set; } = new();
        public decimal TotalHeld { get; set; }
        public decimal TotalRefunded { get; set; }
        public decimal NetBalance { get; set; }
    }
}

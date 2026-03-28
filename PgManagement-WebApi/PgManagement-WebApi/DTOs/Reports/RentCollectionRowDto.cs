namespace PgManagement_WebApi.DTOs.Reports
{
    public class RentCollectionRowDto
    {
        public string RoomNumber { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string TenantPhone { get; set; } = "";
        public decimal ExpectedRent { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string? PaymentMode { get; set; }
        public string Status { get; set; } = ""; // Paid / Partial / Overdue
    }

    public class RentCollectionReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<RentCollectionRowDto> Rows { get; set; } = new();
        public decimal TotalExpected { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPending { get; set; }
    }
}

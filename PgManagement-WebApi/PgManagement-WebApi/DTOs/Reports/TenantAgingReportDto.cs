namespace PgManagement_WebApi.DTOs.Reports
{
    public class TenantAgingRowDto
    {
        public string TenantName { get; set; } = "";
        public string RoomNumber { get; set; } = "";
        public decimal PendingAmount { get; set; }
        public int DaysOverdue { get; set; }
        public string Bucket { get; set; } = "";
    }

    public class TenantAgingBucketDto
    {
        public string Bucket { get; set; } = "";
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TenantAgingReportDto
    {
        public DateTime AsOfDate { get; set; }
        public List<TenantAgingBucketDto> Buckets { get; set; } = new();
        public List<TenantAgingRowDto> Details { get; set; } = new();
        public decimal GrandTotal { get; set; }
    }
}

namespace PgManagement_WebApi.DTOs.Payment
{
    public class PaymentHistoryQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }
        public string? Mode { get; set; }
        public string? TenantId { get; set; }
        public string? UserId { get; set; }

        public string SortBy { get; set; } = "paymentDate";
        public string SortDir { get; set; } = "desc";
    }

}

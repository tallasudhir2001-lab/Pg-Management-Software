namespace PgManagement_WebApi.DTOs.Expense
{
    public class ExpenseListQueryDto
    {

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int? CategoryId { get; set; }

        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        public string? SortBy { get; set; } = "ExpenseDate";
        public string? SortDir { get; set; } = "desc";
    }

}

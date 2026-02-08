namespace PgManagement_WebApi.DTOs.Expense
{
    public class ExpenseCategorySummaryDto
    {
        public int CategoryId { get; set; }
        public string Category { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

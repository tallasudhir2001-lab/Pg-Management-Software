namespace PgManagement_WebApi.DTOs.Expense
{
    public class ExpenseSummaryDto
    {
        public decimal TotalExpense { get; set; }
        public List<ExpenseCategorySummaryDto> CategoryBreakdown { get; set; }
    }
}

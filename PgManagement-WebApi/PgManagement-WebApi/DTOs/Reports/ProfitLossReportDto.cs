namespace PgManagement_WebApi.DTOs.Reports
{
    public class ProfitLossExpenseCategoryDto
    {
        public string Category { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class ProfitLossReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }

        // Revenue
        public decimal TotalRentCollected { get; set; }
        public decimal TotalAdvanceReceived { get; set; }
        public decimal TotalRevenue { get; set; }

        // Expenses
        public decimal TotalExpenses { get; set; }
        public List<ProfitLossExpenseCategoryDto> ExpenseByCategory { get; set; } = new();

        // Net
        public decimal NetProfitOrLoss { get; set; }

        // Collection efficiency
        public decimal? TotalExpectedRent { get; set; }
        public double? CollectionEfficiencyPercent { get; set; }
    }
}

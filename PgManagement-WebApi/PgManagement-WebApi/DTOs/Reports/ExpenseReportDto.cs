namespace PgManagement_WebApi.DTOs.Reports
{
    public class ExpenseReportRowDto
    {
        public DateTime Date { get; set; }
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class ExpenseCategoryGroupDto
    {
        public string Category { get; set; } = "";
        public List<ExpenseReportRowDto> Rows { get; set; } = new();
        public decimal Subtotal { get; set; }
    }

    public class ExpenseReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<ExpenseCategoryGroupDto> Groups { get; set; } = new();
        public decimal GrandTotal { get; set; }
    }
}

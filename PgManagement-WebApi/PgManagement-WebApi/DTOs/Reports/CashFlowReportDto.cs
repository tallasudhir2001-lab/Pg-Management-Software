namespace PgManagement_WebApi.DTOs.Reports
{
    public class CashFlowLineDto
    {
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class CashFlowReportDto
    {
        public int Month { get; set; }
        public int Year { get; set; }

        public List<CashFlowLineDto> Inflows { get; set; } = new();
        public decimal TotalInflows { get; set; }

        public List<CashFlowLineDto> Outflows { get; set; } = new();
        public decimal TotalOutflows { get; set; }

        public decimal NetCashFlow { get; set; }
    }
}

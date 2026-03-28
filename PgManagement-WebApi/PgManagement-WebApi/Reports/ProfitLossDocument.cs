using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class ProfitLossDocument : BaseReportDocument
    {
        private readonly ProfitLossReportDto _data;

        public ProfitLossDocument(ProfitLossReportDto data, PgProfileOptions pg)
            : base(pg, "Monthly Profit & Loss Summary",
                $"{new DateTime(data.Year, data.Month, 1):MMMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            var isProfit = _data.NetProfitOrLoss >= 0;

            container.Column(col =>
            {
                // Revenue section
                col.Item().PaddingTop(10).Text("Revenue").Bold().FontSize(12);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });

                    void Row(string label, decimal amount, bool bold = false)
                    {
                        var labelText = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .Text(label);
                        if (bold) labelText.Bold();
                        var amountText = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .AlignRight().Text($"₹ {amount:N2}");
                        if (bold) amountText.Bold();
                    }

                    Row("Rent Collected", _data.TotalRentCollected);
                    Row("Advance Payments Received", _data.TotalAdvanceReceived);
                    Row("Total Revenue", _data.TotalRevenue, bold: true);
                });

                // Expenses section
                col.Item().PaddingTop(14).Text("Expenses").Bold().FontSize(12);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });

                    foreach (var cat in _data.ExpenseByCategory)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(cat.Category);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .AlignRight().Text($"₹ {cat.Amount:N2}");
                    }

                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("Total Expenses").Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                        .AlignRight().Text($"₹ {_data.TotalExpenses:N2}").Bold();
                });

                // Net
                col.Item().PaddingTop(14).Background(isProfit ? Colors.Green.Lighten4 : Colors.Red.Lighten4)
                    .Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text(isProfit ? "Net Profit" : "Net Loss").Bold().FontSize(13)
                            .FontColor(isProfit ? Colors.Green.Darken3 : Colors.Red.Darken3);
                        row.ConstantItem(150).AlignRight()
                            .Text($"₹ {Math.Abs(_data.NetProfitOrLoss):N2}").Bold().FontSize(15)
                            .FontColor(isProfit ? Colors.Green.Darken3 : Colors.Red.Darken3);
                    });

                if (_data.CollectionEfficiencyPercent.HasValue)
                {
                    col.Item().PaddingTop(8).Text($"Collection Efficiency: {_data.CollectionEfficiencyPercent:F1}%")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                }
            });
        }
    }
}

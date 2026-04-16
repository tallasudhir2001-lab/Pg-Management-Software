using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class CashFlowDocument : BaseReportDocument
    {
        private readonly CashFlowReportDto _data;

        public CashFlowDocument(CashFlowReportDto data, PgProfileOptions pg)
            : base(pg, "Cash Flow Statement",
                $"{new DateTime(data.Year, data.Month, 1):MMMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            var isPositive = _data.NetCashFlow >= 0;

            container.Column(col =>
            {
                // Inflows
                col.Item().PaddingTop(10).Text("Inflows").Bold().FontSize(12).FontColor(Colors.Green.Darken3);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });
                    foreach (var line in _data.Inflows)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(line.Label);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .AlignRight().Text($"₹ {line.Amount:N2}").FontColor(Colors.Green.Darken3);
                    }
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("Total Inflows").Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                        .AlignRight().Text($"₹ {_data.TotalInflows:N2}").Bold().FontColor(Colors.Green.Darken3);
                });

                // Outflows
                col.Item().PaddingTop(14).Text("Outflows").Bold().FontSize(12).FontColor(Colors.Red.Darken3);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(120); });
                    foreach (var line in _data.Outflows)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(line.Label);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                            .AlignRight().Text($"₹ {line.Amount:N2}").FontColor(Colors.Red.Darken3);
                    }
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text("Total Outflows").Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6)
                        .AlignRight().Text($"₹ {_data.TotalOutflows:N2}").Bold().FontColor(Colors.Red.Darken3);
                });

                // Net
                col.Item().PaddingTop(14).Background(isPositive ? Colors.Green.Lighten4 : Colors.Red.Lighten4)
                    .Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text("Net Cash Flow").Bold().FontSize(13)
                            .FontColor(isPositive ? Colors.Green.Darken3 : Colors.Red.Darken3);
                        row.ConstantItem(150).AlignRight()
                            .Text($"₹ {Math.Abs(_data.NetCashFlow):N2}").Bold().FontSize(15)
                            .FontColor(isPositive ? Colors.Green.Darken3 : Colors.Red.Darken3);
                    });
            });
        }
    }
}

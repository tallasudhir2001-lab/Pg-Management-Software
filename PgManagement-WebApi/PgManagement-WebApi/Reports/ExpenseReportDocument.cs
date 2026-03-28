using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class ExpenseReportDocument : BaseReportDocument
    {
        private readonly ExpenseReportDto _data;

        public ExpenseReportDocument(ExpenseReportDto data, PgProfileOptions pg)
            : base(pg, "Monthly Expense Report",
                $"{new DateTime(data.Year, data.Month, 1):MMMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                foreach (var group in _data.Groups)
                {
                    col.Item().PaddingTop(10).Text(group.Category).Bold().FontSize(11);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(3f);
                            c.RelativeColumn(1.5f);
                        });

                        table.Header(h =>
                        {
                            static IContainer S(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4);
                            h.Cell().Element(S).Text("Date").Bold();
                            h.Cell().Element(S).Text("Description").Bold();
                            h.Cell().Element(S).Text("Amount (₹)").Bold();
                        });

                        foreach (var row in group.Rows)
                        {
                            static IContainer Cell(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

                            table.Cell().Element(Cell).Text(row.Date.ToString("dd MMM yyyy"));
                            table.Cell().Element(Cell).Text(row.Description);
                            table.Cell().Element(Cell).AlignRight().Text($"{row.Amount:N2}");
                        }

                        // Subtotal row
                        table.Cell().ColumnSpan(2).Background(Colors.Grey.Lighten4).Padding(4)
                            .AlignRight().Text($"{group.Category} Total").Bold();
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(4)
                            .AlignRight().Text($"{group.Subtotal:N2}").Bold();
                    });
                }

                col.Item().PaddingTop(12).AlignRight()
                    .Text($"Grand Total: ₹ {_data.GrandTotal:N2}").Bold().FontSize(13);
            });
        }
    }
}

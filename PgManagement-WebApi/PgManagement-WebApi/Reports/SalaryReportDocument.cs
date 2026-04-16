using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class SalaryReportDocument : BaseReportDocument
    {
        private readonly SalaryReportDto _data;

        public SalaryReportDocument(SalaryReportDto data, PgProfileOptions pg)
            : base(pg, "Salary Report",
                $"{new DateTime(data.Year, data.Month, 1):MMMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(inner =>
                    {
                        inner.Item().Text("Employees Paid").FontSize(9).FontColor(Colors.Grey.Darken2);
                        inner.Item().Text(_data.EmployeeCount.ToString()).Bold().FontSize(16);
                    });
                    row.ConstantItem(8);
                    row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(inner =>
                    {
                        inner.Item().Text("Total Paid").FontSize(9).FontColor(Colors.Grey.Darken2);
                        inner.Item().Text($"₹ {_data.TotalPaid:N0}").Bold().FontSize(16);
                    });
                });

                if (_data.Rows.Any())
                {
                    col.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(TableHeader).Text("Employee").Bold();
                            h.Cell().Element(TableHeader).Text("Role").Bold();
                            h.Cell().Element(TableHeader).Text("Date").Bold();
                            h.Cell().Element(TableHeader).Text("For Month").Bold();
                            h.Cell().Element(TableHeader).Text("Amount (₹)").Bold();
                            h.Cell().Element(TableHeader).Text("Mode").Bold();
                        });

                        foreach (var row in _data.Rows)
                        {
                            table.Cell().Element(TableCell).Text(row.EmployeeName);
                            table.Cell().Element(TableCell).Text(row.Role);
                            table.Cell().Element(TableCell).Text(row.PaymentDate.ToString("dd MMM yyyy"));
                            table.Cell().Element(TableCell).Text(row.ForMonth);
                            table.Cell().Element(TableCell).AlignRight().Text($"{row.Amount:N0}");
                            table.Cell().Element(TableCell).Text(row.PaymentMode);
                        }
                    });
                }
                else
                {
                    col.Item().PaddingTop(20).AlignCenter().Text("No salary payments this period.").FontColor(Colors.Grey.Darken1);
                }
            });
        }
    }
}

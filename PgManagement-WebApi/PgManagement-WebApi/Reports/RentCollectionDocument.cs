using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class RentCollectionDocument : BaseReportDocument
    {
        private readonly RentCollectionReportDto _data;

        public RentCollectionDocument(RentCollectionReportDto data, PgProfileOptions pg)
            : base(pg, "Monthly Rent Collection Report",
                $"{new DateTime(data.Year, data.Month, 1):MMMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(2f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1f);
                    });

                    table.Header(h =>
                    {
                        static IContainer S(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4).AlignCenter();
                        h.Cell().Element(S).Text("Room").Bold();
                        h.Cell().Element(S).Text("Tenant").Bold();
                        h.Cell().Element(S).Text("Expected (₹)").Bold();
                        h.Cell().Element(S).Text("Paid (₹)").Bold();
                        h.Cell().Element(S).Text("Last Payment").Bold();
                        h.Cell().Element(S).Text("Mode").Bold();
                        h.Cell().Element(S).Text("Status").Bold();
                    });

                    foreach (var row in _data.Rows)
                    {
                        var bg = row.Status switch
                        {
                            "Overdue" => Colors.Red.Lighten4,
                            "Partial" => Colors.Amber.Lighten4,
                            _ => Colors.White
                        };

                        static IContainer Cell(IContainer c, string color) =>
                            c.Background(color).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        table.Cell().Element(c => Cell(c, bg)).Text(row.RoomNumber);
                        table.Cell().Element(c => Cell(c, bg)).Text(row.TenantName);
                        table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"{row.ExpectedRent:N2}");
                        table.Cell().Element(c => Cell(c, bg)).AlignRight().Text($"{row.AmountPaid:N2}");
                        table.Cell().Element(c => Cell(c, bg)).Text(row.LastPaymentDate.HasValue ? row.LastPaymentDate.Value.ToString("dd MMM yyyy") : "—");
                        table.Cell().Element(c => Cell(c, bg)).Text(row.PaymentMode ?? "—");
                        table.Cell().Element(c => Cell(c, bg)).Text(row.Status).Bold()
                            .FontColor(row.Status == "Overdue" ? Colors.Red.Darken2 : row.Status == "Partial" ? Colors.Orange.Darken2 : Colors.Green.Darken2);
                    }
                });

                // Summary
                col.Item().PaddingTop(12).Background(Colors.Grey.Lighten4).Padding(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });

                    void SumCell(string label, string value) =>
                        table.Cell().Column(inner =>
                        {
                            inner.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
                            inner.Item().Text(value).Bold().FontSize(11);
                        });

                    var rate = _data.TotalExpected > 0
                        ? $"{(_data.TotalCollected / _data.TotalExpected * 100):F1}%"
                        : "—";

                    SumCell("Total Expected", $"₹ {_data.TotalExpected:N2}");
                    SumCell("Total Collected", $"₹ {_data.TotalCollected:N2}");
                    SumCell("Total Pending", $"₹ {_data.TotalPending:N2}");
                    SumCell("Collection Rate", rate);
                });
            });
        }
    }
}

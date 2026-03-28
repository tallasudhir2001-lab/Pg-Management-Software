using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class AdvanceBalanceDocument : BaseReportDocument
    {
        private readonly AdvanceBalanceReportDto _data;

        public AdvanceBalanceDocument(AdvanceBalanceReportDto data, PgProfileOptions pg)
            : base(pg, "Advance Balance Report")
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
                        c.RelativeColumn(2f);
                        c.RelativeColumn(1f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                    });

                    table.Header(h =>
                    {
                        static IContainer S(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4);
                        h.Cell().Element(S).Text("Tenant").Bold();
                        h.Cell().Element(S).Text("Room").Bold();
                        h.Cell().Element(S).Text("Advance Paid (₹)").Bold();
                        h.Cell().Element(S).Text("Refunded (₹)").Bold();
                        h.Cell().Element(S).Text("Balance (₹)").Bold();
                        h.Cell().Element(S).Text("Status").Bold();
                    });

                    foreach (var row in _data.Rows)
                    {
                        static IContainer Cell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        table.Cell().Element(Cell).Text(row.TenantName);
                        table.Cell().Element(Cell).Text(row.RoomNumber ?? "—");
                        table.Cell().Element(Cell).AlignRight().Text($"{row.AdvancePaid:N2}");
                        table.Cell().Element(Cell).AlignRight().Text($"{row.AdvanceRefunded:N2}");
                        table.Cell().Element(Cell).AlignRight().Text($"{row.Balance:N2}").Bold();
                        table.Cell().Element(Cell).Text(row.Status);
                    }
                });

                col.Item().PaddingTop(12).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                {
                    void Card(string label, string value)
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
                            inner.Item().Text(value).Bold().FontSize(12);
                        });
                    }
                    Card("Total Advances Held", $"₹ {_data.TotalHeld:N2}");
                    Card("Total Refunded", $"₹ {_data.TotalRefunded:N2}");
                    Card("Net Balance", $"₹ {_data.NetBalance:N2}");
                });
            });
        }
    }
}

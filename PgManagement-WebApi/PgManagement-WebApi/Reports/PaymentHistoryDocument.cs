using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class PaymentHistoryDocument : BaseReportDocument
    {
        private readonly PaymentHistoryReportDto _data;

        public PaymentHistoryDocument(PaymentHistoryReportDto data, PgProfileOptions pg)
            : base(pg, "Payment History Report",
                $"{data.FromDate:dd MMM yyyy} — {data.ToDate:dd MMM yyyy}")
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
                        c.RelativeColumn(1f);
                        c.RelativeColumn(2f);
                        c.RelativeColumn(1f);
                        c.RelativeColumn(1f);
                        c.RelativeColumn(1f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(2f);
                    });

                    table.Header(h =>
                    {
                        static IContainer S(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4);
                        h.Cell().Element(S).Text("Date").Bold();
                        h.Cell().Element(S).Text("Receipt No").Bold();
                        h.Cell().Element(S).Text("Tenant").Bold();
                        h.Cell().Element(S).Text("Room").Bold();
                        h.Cell().Element(S).Text("Type").Bold();
                        h.Cell().Element(S).Text("Mode").Bold();
                        h.Cell().Element(S).Text("Amount (₹)").Bold();
                        h.Cell().Element(S).Text("Remarks").Bold();
                    });

                    foreach (var row in _data.Rows)
                    {
                        static IContainer Cell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        table.Cell().Element(Cell).Text(row.PaymentDate.ToString("dd MMM yyyy"));
                        table.Cell().Element(Cell).Text(row.ReceiptNumber);
                        table.Cell().Element(Cell).Text(row.TenantName);
                        table.Cell().Element(Cell).Text(row.RoomNumber ?? "—");
                        table.Cell().Element(Cell).Text(row.PaymentType);
                        table.Cell().Element(Cell).Text(row.PaymentMode);
                        table.Cell().Element(Cell).AlignRight().Text($"{row.Amount:N2}");
                        table.Cell().Element(Cell).Text(row.Notes ?? "");
                    }
                });

                col.Item().PaddingTop(8).AlignRight()
                    .Text($"Total: ₹ {_data.TotalAmount:N2}").Bold().FontSize(12);
            });
        }
    }
}

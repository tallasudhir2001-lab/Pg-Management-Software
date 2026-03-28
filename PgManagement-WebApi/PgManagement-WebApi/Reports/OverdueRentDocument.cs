using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class OverdueRentDocument : BaseReportDocument
    {
        private readonly OverdueRentReportDto _data;

        public OverdueRentDocument(OverdueRentReportDto data, PgProfileOptions pg)
            : base(pg, "Overdue / Pending Rent Report", $"As of {data.AsOfDate:dd MMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(4).Text($"{_data.TotalOverdueTenants} tenants with outstanding rent — Total: ₹ {_data.TotalOutstanding:N2}")
                    .Bold().FontColor(Colors.Red.Darken2);

                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1f);
                        c.RelativeColumn(2f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                        c.ConstantColumn(60);
                        c.RelativeColumn(1.5f);
                    });

                    table.Header(h =>
                    {
                        static IContainer S(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4);
                        h.Cell().Element(S).Text("Room").Bold();
                        h.Cell().Element(S).Text("Tenant").Bold();
                        h.Cell().Element(S).Text("Phone").Bold();
                        h.Cell().Element(S).Text("Last Payment").Bold();
                        h.Cell().Element(S).Text("Overdue Since").Bold();
                        h.Cell().Element(S).Text("Days").Bold();
                        h.Cell().Element(S).Text("Outstanding (₹)").Bold();
                    });

                    foreach (var row in _data.Rows.OrderByDescending(r => r.DaysOverdue))
                    {
                        static IContainer Cell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        table.Cell().Element(Cell).Text(row.RoomNumber);
                        table.Cell().Element(Cell).Text(row.TenantName);
                        table.Cell().Element(Cell).Text(row.TenantPhone);
                        table.Cell().Element(Cell).Text(row.LastPaymentDate.HasValue ? row.LastPaymentDate.Value.ToString("dd MMM yyyy") : "Never");
                        table.Cell().Element(Cell).Text(row.OverdueSince.ToString("dd MMM yyyy"));
                        table.Cell().Element(Cell).AlignCenter().Text(row.DaysOverdue.ToString()).Bold().FontColor(Colors.Red.Darken2);
                        table.Cell().Element(Cell).AlignRight().Text($"{row.OutstandingAmount:N2}").Bold();
                    }
                });
            });
        }
    }
}

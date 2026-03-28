using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class TenantListDocument : BaseReportDocument
    {
        private readonly TenantListReportDto _data;

        public TenantListDocument(TenantListReportDto data, PgProfileOptions pg)
            : base(pg, $"Tenant List — {data.StatusFilter}")
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
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.8f);
                        c.RelativeColumn(1f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.2f);
                    });

                    table.Header(h =>
                    {
                        static IContainer S(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4);
                        h.Cell().Element(S).Text("Name").Bold();
                        h.Cell().Element(S).Text("Phone").Bold();
                        h.Cell().Element(S).Text("Aadhaar").Bold();
                        h.Cell().Element(S).Text("Room").Bold();
                        h.Cell().Element(S).Text("Check-in").Bold();
                        if (_data.StatusFilter == "Moved Out")
                            h.Cell().Element(S).Text("Move-out").Bold();
                        else
                            h.Cell().Element(S).Text("Status").Bold();
                        h.Cell().Element(S).Text("Rent/mo (₹)").Bold();
                    });

                    foreach (var row in _data.Rows)
                    {
                        static IContainer Cell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        table.Cell().Element(Cell).Text(row.TenantName);
                        table.Cell().Element(Cell).Text(row.Phone);
                        table.Cell().Element(Cell).Text(row.AadhaarMasked);
                        table.Cell().Element(Cell).Text(row.RoomNumber ?? "—");
                        table.Cell().Element(Cell).Text(row.CheckInDate.HasValue ? row.CheckInDate.Value.ToString("dd MMM yyyy") : "—");
                        if (_data.StatusFilter == "Moved Out")
                            table.Cell().Element(Cell).Text(row.MoveOutDate.HasValue ? row.MoveOutDate.Value.ToString("dd MMM yyyy") : "—");
                        else
                            table.Cell().Element(Cell).Text(row.Status);
                        table.Cell().Element(Cell).AlignRight().Text($"{row.MonthlyRent:N2}");
                    }
                });
            });
        }
    }
}

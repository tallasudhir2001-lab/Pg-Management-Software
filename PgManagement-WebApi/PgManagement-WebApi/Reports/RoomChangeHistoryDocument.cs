using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class RoomChangeHistoryDocument : BaseReportDocument
    {
        private readonly RoomChangeHistoryReportDto _data;

        public RoomChangeHistoryDocument(RoomChangeHistoryReportDto data, PgProfileOptions pg)
            : base(pg, "Room Change History",
                $"{data.FromDate:dd MMM yyyy} – {data.ToDate:dd MMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(10).Text($"Total room changes: {_data.TotalChanges}").Bold().FontSize(11);

                if (_data.Rows.Any())
                {
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(TableHeader).Text("Tenant").Bold();
                            h.Cell().Element(TableHeader).Text("Date").Bold();
                            h.Cell().Element(TableHeader).Text("Old Room").Bold();
                            h.Cell().Element(TableHeader).Text("New Room").Bold();
                            h.Cell().Element(TableHeader).Text("Old Rent (₹)").Bold();
                            h.Cell().Element(TableHeader).Text("New Rent (₹)").Bold();
                        });

                        foreach (var row in _data.Rows)
                        {
                            table.Cell().Element(TableCell).Text(row.TenantName);
                            table.Cell().Element(TableCell).Text(row.ChangeDate.ToString("dd MMM yyyy"));
                            table.Cell().Element(TableCell).Text(row.OldRoom);
                            table.Cell().Element(TableCell).Text(row.NewRoom);
                            table.Cell().Element(TableCell).AlignRight().Text($"{row.OldRent:N0}");
                            table.Cell().Element(TableCell).AlignRight().Text($"{row.NewRent:N0}");
                        }
                    });
                }
                else
                {
                    col.Item().PaddingTop(20).AlignCenter().Text("No room changes in this period.").FontColor(Colors.Grey.Darken1);
                }
            });
        }
    }
}

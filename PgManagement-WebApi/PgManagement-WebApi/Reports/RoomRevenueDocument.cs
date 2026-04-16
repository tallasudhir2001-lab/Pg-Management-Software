using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class RoomRevenueDocument : BaseReportDocument
    {
        private readonly RoomRevenueReportDto _data;

        public RoomRevenueDocument(RoomRevenueReportDto data, PgProfileOptions pg)
            : base(pg, "Room Revenue Report",
                $"{new DateTime(data.Year, data.Month, 1):MMMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(0.6f);
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn(0.8f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(1.2f);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(TableHeader).Text("Room").Bold();
                        h.Cell().Element(TableHeader).Text("Beds").Bold();
                        h.Cell().Element(TableHeader).Text("Occ. Days").Bold();
                        h.Cell().Element(TableHeader).Text("Vacant").Bold();
                        h.Cell().Element(TableHeader).Text("Rent (₹)").Bold();
                        h.Cell().Element(TableHeader).Text("Expense (₹)").Bold();
                        h.Cell().Element(TableHeader).Text("Net (₹)").Bold();
                    });

                    foreach (var row in _data.Rows)
                    {
                        table.Cell().Element(TableCell).Text(row.RoomNumber);
                        table.Cell().Element(TableCell).AlignRight().Text(row.Capacity.ToString());
                        table.Cell().Element(TableCell).AlignRight().Text(row.OccupiedDays.ToString());
                        table.Cell().Element(TableCell).AlignRight().Text(row.VacantDays.ToString());
                        table.Cell().Element(TableCell).AlignRight().Text($"{row.RentCollected:N0}");
                        table.Cell().Element(TableCell).AlignRight().Text($"{row.ExpenseAllocated:N0}");
                        table.Cell().Element(TableCell).AlignRight()
                            .Text($"{row.NetRevenue:N0}")
                            .FontColor(row.NetRevenue >= 0 ? Colors.Green.Darken3 : Colors.Red.Darken3);
                    }
                });

                col.Item().PaddingTop(12).AlignRight()
                    .Text($"Total Net Revenue: ₹ {_data.TotalNetRevenue:N2}").Bold().FontSize(13);
            });
        }
    }
}

using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class OccupancyDocument : BaseReportDocument
    {
        private readonly OccupancyReportDto _data;

        public OccupancyDocument(OccupancyReportDto data, PgProfileOptions pg)
            : base(pg, "Room Occupancy Report", $"As of {data.AsOfDate:dd MMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // Summary cards
                col.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                {
                    void Card(string label, string value)
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
                            inner.Item().Text(value).Bold().FontSize(13);
                        });
                    }
                    Card("Total Rooms", _data.TotalRooms.ToString());
                    Card("Total Beds", _data.TotalBeds.ToString());
                    Card("Occupied", _data.TotalOccupied.ToString());
                    Card("Vacant", _data.TotalVacant.ToString());
                    Card("Occupancy %", $"{_data.OverallOccupancyPercent:F1}%");
                });

                col.Item().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1f);
                        c.ConstantColumn(70);
                        c.ConstantColumn(70);
                        c.ConstantColumn(70);
                        c.ConstantColumn(80);
                        c.RelativeColumn(3f);
                    });

                    table.Header(h =>
                    {
                        static IContainer S(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4);
                        h.Cell().Element(S).Text("Room").Bold();
                        h.Cell().Element(S).Text("Total").Bold();
                        h.Cell().Element(S).Text("Occupied").Bold();
                        h.Cell().Element(S).Text("Vacant").Bold();
                        h.Cell().Element(S).Text("Occ %").Bold();
                        h.Cell().Element(S).Text("Tenants").Bold();
                    });

                    foreach (var row in _data.Rows)
                    {
                        static IContainer Cell(IContainer c) =>
                            c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);

                        table.Cell().Element(Cell).Text(row.RoomNumber);
                        table.Cell().Element(Cell).AlignCenter().Text(row.TotalBeds.ToString());
                        table.Cell().Element(Cell).AlignCenter().Text(row.OccupiedBeds.ToString());
                        table.Cell().Element(Cell).AlignCenter().Text(row.VacantBeds.ToString());
                        table.Cell().Element(Cell).AlignCenter().Text($"{row.OccupancyPercent:F0}%");
                        table.Cell().Element(Cell).Text(row.TenantNames);
                    }
                });
            });
        }
    }
}

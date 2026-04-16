using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class TenantAgingDocument : BaseReportDocument
    {
        private readonly TenantAgingReportDto _data;

        public TenantAgingDocument(TenantAgingReportDto data, PgProfileOptions pg)
            : base(pg, "Tenant Aging Report",
                $"As of {data.AsOfDate:dd MMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // Bucket summary
                col.Item().PaddingTop(10).Row(row =>
                {
                    foreach (var bucket in _data.Buckets)
                    {
                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(8).Column(inner =>
                        {
                            inner.Item().Text(bucket.Bucket).FontSize(9).FontColor(Colors.Grey.Darken2);
                            inner.Item().Text($"{bucket.Count} tenants").Bold().FontSize(12);
                            inner.Item().Text($"₹ {bucket.TotalAmount:N0}").FontSize(10).FontColor(Colors.Red.Darken2);
                        });
                        row.ConstantItem(6);
                    }
                });

                col.Item().PaddingTop(6).AlignRight()
                    .Text($"Total Overdue: ₹ {_data.GrandTotal:N2}").Bold().FontSize(13).FontColor(Colors.Red.Darken3);

                if (_data.Details.Any())
                {
                    col.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(TableHeader).Text("Tenant").Bold();
                            h.Cell().Element(TableHeader).Text("Room").Bold();
                            h.Cell().Element(TableHeader).Text("Bucket").Bold();
                            h.Cell().Element(TableHeader).Text("Amount (₹)").Bold();
                            h.Cell().Element(TableHeader).Text("Days").Bold();
                        });

                        foreach (var row in _data.Details)
                        {
                            var color = row.DaysOverdue > 30 ? Colors.Red.Darken3
                                : row.DaysOverdue > 15 ? Colors.Orange.Darken2
                                : Colors.Black;

                            table.Cell().Element(TableCell).Text(row.TenantName);
                            table.Cell().Element(TableCell).Text(row.RoomNumber);
                            table.Cell().Element(TableCell).Text(row.Bucket);
                            table.Cell().Element(TableCell).AlignRight().Text($"{row.PendingAmount:N0}").FontColor(Colors.Red.Darken2);
                            table.Cell().Element(TableCell).AlignRight().Text(row.DaysOverdue.ToString()).FontColor(color);
                        }
                    });
                }
            });
        }
    }
}

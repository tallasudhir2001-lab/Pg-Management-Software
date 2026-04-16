using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class TenantTurnoverDocument : BaseReportDocument
    {
        private readonly TenantTurnoverReportDto _data;

        public TenantTurnoverDocument(TenantTurnoverReportDto data, PgProfileOptions pg)
            : base(pg, "Tenant Turnover Report",
                $"{new DateTime(data.Year, data.Month, 1):MMMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // Summary
                col.Item().PaddingTop(10).Row(row =>
                {
                    void Stat(IContainer c, string label, string value)
                    {
                        c.Background(Colors.Grey.Lighten4).Padding(10).Column(inner =>
                        {
                            inner.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken2);
                            inner.Item().Text(value).Bold().FontSize(16);
                        });
                    }

                    row.RelativeItem().Element(c => Stat(c, "Move-Ins", _data.MoveIns.ToString()));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => Stat(c, "Move-Outs", _data.MoveOuts.ToString()));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => Stat(c, "Avg Stay (days)", _data.AverageStayDays.ToString("F0")));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => Stat(c, "Churn Rate", $"{_data.ChurnRatePercent:F1}%"));
                });

                if (_data.MoveOutDetails.Any())
                {
                    col.Item().PaddingTop(14).Text("Move-Out Details").Bold().FontSize(12);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2f);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(0.8f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(TableHeader).Text("Tenant").Bold();
                            h.Cell().Element(TableHeader).Text("Room").Bold();
                            h.Cell().Element(TableHeader).Text("Check-In").Bold();
                            h.Cell().Element(TableHeader).Text("Check-Out").Bold();
                            h.Cell().Element(TableHeader).Text("Days").Bold();
                        });

                        foreach (var row in _data.MoveOutDetails)
                        {
                            table.Cell().Element(TableCell).Text(row.TenantName);
                            table.Cell().Element(TableCell).Text(row.RoomNumber);
                            table.Cell().Element(TableCell).Text(row.CheckInDate.ToString("dd MMM yyyy"));
                            table.Cell().Element(TableCell).Text(row.CheckOutDate.ToString("dd MMM yyyy"));
                            table.Cell().Element(TableCell).AlignRight().Text(row.StayDays.ToString());
                        }
                    });
                }
                else
                {
                    col.Item().PaddingTop(20).AlignCenter().Text("No move-outs this period.").FontColor(Colors.Grey.Darken1);
                }
            });
        }
    }
}

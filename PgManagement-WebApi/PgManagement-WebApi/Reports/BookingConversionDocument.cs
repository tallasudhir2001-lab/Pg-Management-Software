using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class BookingConversionDocument : BaseReportDocument
    {
        private readonly BookingConversionReportDto _data;

        public BookingConversionDocument(BookingConversionReportDto data, PgProfileOptions pg)
            : base(pg, "Booking Conversion Report",
                $"{data.FromDate:dd MMM yyyy} – {data.ToDate:dd MMM yyyy}")
        {
            _data = data;
        }

        protected override void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(10).Row(row =>
                {
                    void Stat(IContainer c, string label, string value, string color)
                    {
                        c.Background(Colors.Grey.Lighten4).Padding(10).Column(inner =>
                        {
                            inner.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken2);
                            inner.Item().Text(value).Bold().FontSize(18);
                        });
                    }

                    row.RelativeItem().Element(c => Stat(c, "Total Bookings", _data.TotalBookings.ToString(), ""));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => Stat(c, "Checked In", _data.CheckedIn.ToString(), ""));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => Stat(c, "Cancelled", _data.Cancelled.ToString(), ""));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => Stat(c, "Expired", _data.Expired.ToString(), ""));
                    row.ConstantItem(8);
                    row.RelativeItem().Element(c => Stat(c, "Still Active", _data.StillActive.ToString(), ""));
                });

                var cvColor = _data.ConversionRatePercent >= 70 ? Colors.Green.Darken3 : Colors.Orange.Darken2;
                col.Item().PaddingTop(14).Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
                {
                    row.RelativeItem().Text("Conversion Rate").Bold().FontSize(13);
                    row.ConstantItem(150).AlignRight()
                        .Text($"{_data.ConversionRatePercent:F1}%").Bold().FontSize(18).FontColor(cvColor);
                });
            });
        }
    }
}

using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public class PaymentReceiptDocument : IDocument
    {
        private readonly ReceiptDataDto _data;
        private readonly PgProfileOptions _pgProfile;

        public PaymentReceiptDocument(ReceiptDataDto data, PgProfileOptions pgProfile)
        {
            _data = data;
            _pgProfile = pgProfile;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Element(ComposeContent);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(col =>
            {
                // PG Header
                col.Item().AlignCenter().Column(inner =>
                {
                    inner.Item().Text(_pgProfile.Name).Bold().FontSize(14).AlignCenter();
                    inner.Item().Text(_pgProfile.Address).FontSize(9).FontColor(Colors.Grey.Darken2).AlignCenter();
                    inner.Item().Text($"Ph: {_pgProfile.Phone}").FontSize(9).FontColor(Colors.Grey.Darken2).AlignCenter();
                });

                col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                // Receipt Title
                col.Item().Text("PAYMENT RECEIPT").Bold().FontSize(13).AlignCenter();

                col.Item().PaddingVertical(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                // Receipt Meta
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Receipt No: {_data.ReceiptNumber}").Bold().FontSize(10);
                    row.RelativeItem().AlignRight().Text($"Date: {_data.PaymentDate:dd MMM yyyy}").FontSize(10);
                });

                col.Item().PaddingTop(10).Text("Tenant Details").Bold().FontSize(11);
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                    });

                    void AddRow(string label, string? value)
                    {
                        table.Cell().Padding(3).Text(label).FontColor(Colors.Grey.Darken2);
                        table.Cell().Padding(3).Text(value ?? "—");
                    }

                    AddRow("Name", _data.TenantName);
                    AddRow("Phone", _data.TenantPhone);
                    AddRow("Room", _data.RoomNumber != null ? $"Room {_data.RoomNumber}" : "—");
                });

                col.Item().PaddingTop(10).Text("Payment Details").Bold().FontSize(11);
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                    });

                    void AddRow(string label, string? value)
                    {
                        table.Cell().Padding(3).Text(label).FontColor(Colors.Grey.Darken2);
                        table.Cell().Padding(3).Text(value ?? "—");
                    }

                    AddRow("Type", _data.PaymentType);
                    AddRow("Mode", _data.PaymentMode);
                    AddRow("Period", $"{_data.PaidFrom:dd MMM yyyy} → {_data.PaidUpto:dd MMM yyyy}");
                    if (!string.IsNullOrEmpty(_data.Notes))
                        AddRow("Remarks", _data.Notes);
                });

                // Amount (prominent)
                col.Item().PaddingTop(14).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("Amount Paid").Bold().FontSize(12);
                    row.RelativeItem().AlignRight().Text($"₹ {_data.Amount:N2}").Bold().FontSize(16).FontColor(Colors.Green.Darken2);
                });

                // Footer
                col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                col.Item().PaddingTop(6).Text("This is a computer-generated receipt. No signature required.")
                    .FontSize(8).FontColor(Colors.Grey.Darken1).AlignCenter().Italic();
            });
        }
    }
}

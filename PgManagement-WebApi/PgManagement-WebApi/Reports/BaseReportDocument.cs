using PgManagement_WebApi.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PgManagement_WebApi.Reports
{
    public abstract class BaseReportDocument : IDocument
    {
        protected readonly PgProfileOptions _pgProfile;
        protected readonly string _reportTitle;
        protected readonly string? _dateRange;

        protected BaseReportDocument(PgProfileOptions pgProfile, string reportTitle, string? dateRange = null)
        {
            _pgProfile = pgProfile;
            _reportTitle = reportTitle;
            _dateRange = dateRange;
        }

        public virtual DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(inner =>
                    {
                        inner.Item().Text(_pgProfile.Name).Bold().FontSize(14);
                        inner.Item().Text(_pgProfile.Address).FontSize(9).FontColor(Colors.Grey.Darken2);
                        inner.Item().Text($"Ph: {_pgProfile.Phone}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(150).AlignRight().Column(inner =>
                    {
                        inner.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
                });

                col.Item().PaddingTop(6).Text(_reportTitle).Bold().FontSize(14).AlignCenter();

                if (!string.IsNullOrEmpty(_dateRange))
                    col.Item().Text(_dateRange).FontSize(10).FontColor(Colors.Grey.Darken1).AlignCenter();

                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
        }

        protected abstract void ComposeContent(IContainer container);

        private void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Darken1);
                    text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        protected static IContainer TableHeader(IContainer container) =>
            container.Background(Colors.Grey.Lighten3).Padding(4);

        protected static IContainer TableCell(IContainer container) =>
            container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4);
    }
}

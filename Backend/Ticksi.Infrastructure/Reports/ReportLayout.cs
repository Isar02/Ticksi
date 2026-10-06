using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ticksi.Infrastructure.Reports;

internal static class ReportLayout
{
    public static readonly string Accent = Colors.Blue.Darken2;

    public static void Page(IDocumentContainer container, Action<IContainer> header, Action<IContainer> content)
    {
        container.Page(page =>
        {
            page.Margin(50);
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            page.Header().Element(header);
            page.Content().Element(content);
            page.Footer().Element(ComposeFooter);
        });
    }

    public static void Header(IContainer container, string title, IEnumerable<string> details, DateTimeOffset generatedAt)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(10).Column(col =>
            {
                col.Item().Text(title).FontSize(16).Bold().FontColor(Accent);

                foreach (var detail in details)
                    col.Item().PaddingTop(5).Text(detail).FontSize(10);

                col.Item().PaddingTop(2).Text($"Generated: {ReportFormat.DateTime(generatedAt.DateTime)}")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });

            column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    public static IContainer HeaderCell(IContainer cell) => cell.Background(Accent).Padding(8);

    public static IContainer BodyCell(IContainer cell, int rowIndex) =>
        cell.Background(rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten3).Padding(8);

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken1));
            text.Span("Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }
}

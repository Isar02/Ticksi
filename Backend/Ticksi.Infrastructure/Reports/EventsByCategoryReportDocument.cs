using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Ticksi.Application.Features.Reports.Models;

namespace Ticksi.Infrastructure.Reports;

public sealed class EventsByCategoryReportDocument : IDocument
{
    private readonly EventsByCategoryReport _report;

    public EventsByCategoryReportDocument(EventsByCategoryReport report)
    {
        _report = report;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(10).Column(col =>
            {
                col.Item().Text($"EVENTS REPORT - {_report.CategoryName.ToUpper(ReportFormat.Culture)}")
                    .FontSize(16)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                col.Item().PaddingTop(5).Text($"Period: {ReportFormat.Period(_report.DateFrom, _report.DateTo)}")
                    .FontSize(10);

                col.Item().PaddingTop(2).Text($"Generated: {ReportFormat.DateTime(_report.GeneratedAt.DateTime)}")
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            });

            column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(20).Column(column =>
        {
            if (_report.Events.Count == 0)
            {
                column.Item().AlignCenter().Text("No events found for this category in the chosen period.")
                    .FontSize(12)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1.5f);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Event Name").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Date").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Location").FontColor(Colors.White).Bold();
                    header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("From").FontColor(Colors.White).Bold();
                });

                var rowIndex = 0;
                foreach (var eventItem in _report.Events)
                {
                    var backgroundColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                    table.Cell().Background(backgroundColor).Padding(8).Text(eventItem.Name);
                    table.Cell().Background(backgroundColor).Padding(8).Text(ReportFormat.DateTime(eventItem.Date));

                    var locationText = $"{eventItem.LocationName}\n{eventItem.City}\n{eventItem.Address}";
                    table.Cell().Background(backgroundColor).Padding(8).Text(locationText).FontSize(9);

                    table.Cell().Background(backgroundColor).Padding(8).Text(ReportFormat.Price(eventItem.LowestPrice));

                    rowIndex++;
                }
            });

            column.Item().PaddingTop(20).Text($"Total Events: {_report.Events.Count}")
                .Bold()
                .FontSize(11);
        });
    }

    private void ComposeFooter(IContainer container)
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

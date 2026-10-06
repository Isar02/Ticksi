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

    public void Compose(IDocumentContainer container) =>
        ReportLayout.Page(container, ComposeHeader, ComposeContent);

    private void ComposeHeader(IContainer container) =>
        ReportLayout.Header(
            container,
            $"EVENTS REPORT - {_report.CategoryName.ToUpper(ReportFormat.Culture)}",
            [$"Period: {ReportFormat.Period(_report.DateFrom, _report.DateTo)}"],
            _report.GeneratedAt);

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
                    foreach (var title in new[] { "Event Name", "Date", "Location", "From" })
                        header.Cell().Element(ReportLayout.HeaderCell).Text(title).FontColor(Colors.White).Bold();
                });

                foreach (var (eventItem, row) in _report.Events.Select((e, i) => (e, i)))
                {
                    table.Cell().Element(cell => ReportLayout.BodyCell(cell, row)).Text(eventItem.Name);
                    table.Cell().Element(cell => ReportLayout.BodyCell(cell, row)).Text(ReportFormat.DateTime(eventItem.Date));

                    var locationText = $"{eventItem.LocationName}\n{eventItem.City}\n{eventItem.Address}";
                    table.Cell().Element(cell => ReportLayout.BodyCell(cell, row)).Text(locationText).FontSize(9);

                    table.Cell().Element(cell => ReportLayout.BodyCell(cell, row)).Text(ReportFormat.Price(eventItem.LowestPrice));
                }
            });

            column.Item().PaddingTop(20).Text($"Total Events: {_report.Events.Count}")
                .Bold()
                .FontSize(11);
        });
    }
}

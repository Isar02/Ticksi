using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Ticksi.Application.Features.Reports.Models;

namespace Ticksi.Infrastructure.Reports;

public sealed class EventSalesReportDocument : IDocument
{
    private readonly EventSalesReport _report;

    public EventSalesReportDocument(EventSalesReport report)
    {
        _report = report;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container) =>
        ReportLayout.Page(container, ComposeHeader, ComposeContent);

    private void ComposeHeader(IContainer container) =>
        ReportLayout.Header(
            container,
            $"SALES REPORT - {_report.EventName.ToUpper(ReportFormat.Culture)}",
            [
                $"Event: {ReportFormat.DateTime(_report.EventDate)}, {_report.VenueName}, {_report.VenueCity}",
                $"Sales period: {ReportFormat.Period(_report.DateFrom, _report.DateTo)}"
            ],
            _report.GeneratedAt);

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(8);

            column.Item().Row(row =>
            {
                row.Spacing(12);
                row.RelativeItem().Element(cell => ComposeTotal(cell, "Tickets sold", ReportFormat.Count(_report.TicketsSold)));
                row.RelativeItem().Element(cell => ComposeTotal(cell, "Revenue", ReportFormat.Price(_report.Revenue)));
            });

            column.Item().PaddingTop(16).Element(cell => ComposeHeading(cell, "By ticket type"));
            column.Item().Element(cell => ComposeSalesTable(
                cell,
                "Ticket type",
                _report.TicketTypes.Select(t => (t.Name, t.TicketsSold, t.Revenue))));

            column.Item().PaddingTop(16).Element(cell => ComposeHeading(cell, "By day"));
            if (_report.Days.Count == 0)
            {
                column.Item().Text("No paid orders in the chosen period.")
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().Element(cell => ComposeSalesTable(
                cell,
                "Date",
                _report.Days.Select(d => (ReportFormat.Date(d.Date), d.TicketsSold, d.Revenue))));
        });
    }

    private static void ComposeTotal(IContainer container, string label, string value)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(12).Column(column =>
        {
            column.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().PaddingTop(4).Text(value).FontSize(16).Bold().FontColor(ReportLayout.Accent);
        });
    }

    private static void ComposeHeading(IContainer container, string title) =>
        container.Text(title).FontSize(12).Bold();

    private static void ComposeSalesTable(
        IContainer container,
        string firstColumn,
        IEnumerable<(string Label, int TicketsSold, decimal Revenue)> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Element(ReportLayout.HeaderCell).Text(firstColumn).FontColor(Colors.White).Bold();
                header.Cell().Element(ReportLayout.HeaderCell).AlignRight().Text("Sold").FontColor(Colors.White).Bold();
                header.Cell().Element(ReportLayout.HeaderCell).AlignRight().Text("Revenue").FontColor(Colors.White).Bold();
            });

            foreach (var ((label, ticketsSold, revenue), row) in rows.Select((r, i) => (r, i)))
            {
                table.Cell().Element(cell => ReportLayout.BodyCell(cell, row)).Text(label);
                table.Cell().Element(cell => ReportLayout.BodyCell(cell, row)).AlignRight().Text(ReportFormat.Count(ticketsSold));
                table.Cell().Element(cell => ReportLayout.BodyCell(cell, row)).AlignRight().Text(ReportFormat.Price(revenue));
            }
        });
    }
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Reports.Documents
{
    public class EventsByCategoryReportDocument : IDocument
    {
        private readonly string _categoryName;
        private readonly List<Event> _events;

        public EventsByCategoryReportDocument(string categoryName, List<Event> events)
        {
            _categoryName = categoryName;
            _events = events;
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
                column.Item().PaddingBottom(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"EVENTS REPORT - {_categoryName.ToUpper()}")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        col.Item().PaddingTop(5).Text($"Generated: {DateTime.Now:MMMM dd, yyyy}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(20).Column(column =>
            {
                if (!_events.Any())
                {
                    column.Item().AlignCenter().Text("No events found for this category.")
                        .FontSize(12)
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);
                    return;
                }

                // Table header
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); // Event Name
                        columns.RelativeColumn(2); // Date
                        columns.RelativeColumn(3); // Location
                        columns.RelativeColumn(1); // Price
                    });

                    // Header row
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Event Name").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Date").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Location").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken2).Padding(8).Text("Price").FontColor(Colors.White).Bold();
                    });

                    // Data rows
                    var rowIndex = 0;
                    foreach (var eventItem in _events)
                    {
                        var backgroundColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                        table.Cell().Background(backgroundColor).Padding(8).Text(eventItem.Name);
                        table.Cell().Background(backgroundColor).Padding(8).Text(eventItem.Date.ToString("yyyy-MM-dd"));
                        
                        // Format location with name, city, and address
                        var locationText = eventItem.Location != null
                            ? $"{eventItem.Location.Name}\n{eventItem.Location.City}\n{eventItem.Location.Address}"
                            : "N/A";
                        table.Cell().Background(backgroundColor).Padding(8).Text(locationText).FontSize(9);
                        
                        table.Cell().Background(backgroundColor).Padding(8).Text($"${eventItem.Price:F2}");

                        rowIndex++;
                    }
                });

                // Summary section
                column.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem().Text($"Total Events: {_events.Count}")
                        .Bold()
                        .FontSize(11);
                });
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
}


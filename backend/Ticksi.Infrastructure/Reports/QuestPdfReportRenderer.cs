using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Ticksi.Application.Features.Reports.Models;
using Ticksi.Application.Interfaces;

namespace Ticksi.Infrastructure.Reports;

public sealed class QuestPdfReportRenderer : IReportRenderer
{
    static QuestPdfReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] RenderEventsByCategory(EventsByCategoryReport report) =>
        new EventsByCategoryReportDocument(report).GeneratePdf();

    public byte[] RenderEventSales(EventSalesReport report) =>
        new EventSalesReportDocument(report).GeneratePdf();
}

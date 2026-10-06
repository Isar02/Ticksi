using Ticksi.Application.Features.Reports.Models;
using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.ReportTests.UnitTests;

internal sealed class CapturingReportRenderer : IReportRenderer
{
    public static readonly byte[] Output = [1, 2, 3];

    public EventsByCategoryReport? EventsByCategory { get; private set; }
    public EventSalesReport? EventSales { get; private set; }

    public byte[] RenderEventsByCategory(EventsByCategoryReport report)
    {
        EventsByCategory = report;
        return Output;
    }

    public byte[] RenderEventSales(EventSalesReport report)
    {
        EventSales = report;
        return Output;
    }
}

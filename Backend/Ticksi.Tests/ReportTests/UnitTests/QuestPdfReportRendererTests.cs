using System.Text;
using Ticksi.Application.Features.Reports.Models;
using Ticksi.Infrastructure.Reports;

namespace Ticksi.Tests.ReportTests.UnitTests;

public class QuestPdfReportRendererTests
{
    [Fact]
    public void Format_UsesRegionalDatesAndPricesInKm()
    {
        Assert.Equal("6. 1. 2027. 18:00", ReportFormat.DateTime(new DateTime(2027, 1, 6, 18, 0, 0)));
        Assert.Equal("1.234,50 KM", ReportFormat.Price(1234.5m));
        Assert.Equal("1.234", ReportFormat.Count(1234));
        Assert.Equal("-", ReportFormat.Price(null));
    }

    [Theory]
    [InlineData("2026-10-01", "2026-10-31", "1. 10. 2026. – 31. 10. 2026.")]
    [InlineData("2026-10-01", null, "From 1. 10. 2026.")]
    [InlineData(null, "2026-10-31", "Until 31. 10. 2026.")]
    [InlineData(null, null, "All dates")]
    public void Format_Period_DescribesTheRange(string? from, string? to, string expected)
    {
        Assert.Equal(expected, ReportFormat.Period(
            from is null ? null : DateOnly.Parse(from),
            to is null ? null : DateOnly.Parse(to)));
    }

    [Fact]
    public void RenderEventsByCategory_ProducesAPdf()
    {
        var report = new EventsByCategoryReport(
            "Music",
            new DateOnly(2026, 10, 1),
            null,
            new DateTimeOffset(2026, 10, 5, 22, 15, 0, TimeSpan.FromHours(2)),
            [new EventsByCategoryReportRow("Jazz Night", new DateTime(2026, 10, 8, 20, 0, 0), "Zetra", "Sarajevo", "Alipašina bb", 25m)]);

        var pdf = new QuestPdfReportRenderer().RenderEventsByCategory(report);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void RenderEventSales_WithAndWithoutSales_ProducesAPdf()
    {
        var sold = new EventSalesReport(
            "Jazz Night",
            new DateTime(2026, 10, 8, 20, 0, 0),
            "Zetra",
            "Sarajevo",
            null,
            new DateOnly(2026, 10, 6),
            new DateTimeOffset(2026, 10, 6, 11, 0, 0, TimeSpan.FromHours(2)),
            [new TicketTypeSales("Standard", 3, 75m), new TicketTypeSales("VIP", 0, 0m)],
            [new DailySales(new DateOnly(2026, 10, 2), 3, 75m)]);
        var unsold = sold with { TicketTypes = [new TicketTypeSales("Standard", 0, 0m)], Days = [] };
        var renderer = new QuestPdfReportRenderer();

        Assert.All(
            [renderer.RenderEventSales(sold), renderer.RenderEventSales(unsold)],
            pdf => Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4)));
    }
}

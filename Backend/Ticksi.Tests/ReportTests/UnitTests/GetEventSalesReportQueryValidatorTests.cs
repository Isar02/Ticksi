using Ticksi.Application.Features.Reports.Queries.GetEventSalesReport;

namespace Ticksi.Tests.ReportTests.UnitTests;

public class GetEventSalesReportQueryValidatorTests
{
    private readonly GetEventSalesReportQueryValidator _validator = new();

    [Theory]
    [InlineData(null, null)]
    [InlineData("2026-10-01", null)]
    [InlineData(null, "2026-10-31")]
    [InlineData("2026-10-01", "2026-10-01")]
    public async Task Validate_OpenOrOrderedRange_Passes(string? from, string? to)
    {
        var result = await _validator.ValidateAsync(Query(from, to));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EndBeforeStartAndNoEvent_ReportsEachField()
    {
        var query = Query("2026-10-02", "2026-10-01");
        query.EventPublicId = Guid.Empty;

        var result = await _validator.ValidateAsync(query);

        Assert.Equal(["DateTo", "EventPublicId"], result.Errors.Select(e => e.PropertyName).Order());
    }

    private static GetEventSalesReportQuery Query(string? from, string? to) => new()
    {
        EventPublicId = Guid.NewGuid(),
        DateFrom = from is null ? null : DateOnly.Parse(from),
        DateTo = to is null ? null : DateOnly.Parse(to)
    };
}

using Ticksi.Application.Common;
using Ticksi.Application.Features.Events.Queries.GetManagedEvents;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetManagedEventsQueryValidatorTests
{
    private readonly GetManagedEventsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_EveryFilterAndSortSet_Passes()
    {
        var query = new GetManagedEventsQuery
        {
            Name = "Jazz",
            CategoryId = Guid.NewGuid(),
            LocationId = Guid.NewGuid(),
            DateFrom = new DateOnly(2026, 12, 1),
            DateTo = new DateOnly(2026, 12, 1),
            Period = "UPCOMING",
            SortBy = "Venue",
            SortDescending = true,
            Page = 3,
            PageSize = PagingExtensions.MaxPageSize
        };

        var result = await _validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_UnknownValuesAndOversizedPage_ReportsEachField()
    {
        var query = new GetManagedEventsQuery
        {
            DateFrom = new DateOnly(2026, 12, 2),
            DateTo = new DateOnly(2026, 12, 1),
            Period = "soon",
            SortBy = "price",
            Page = 0,
            PageSize = PagingExtensions.MaxPageSize + 1
        };

        var result = await _validator.ValidateAsync(query);

        Assert.Equal(
            ["DateTo", "Page", "PageSize", "Period", "SortBy"],
            result.Errors.Select(e => e.PropertyName).Order());
    }
}

using Ticksi.Application.Features.Events.Queries.GetEvents;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetEventsQueryValidatorTests
{
    private readonly GetEventsQueryValidator _validator = new();

    [Fact]
    public async Task Validate_CityAtItsLimitAfterTrimming_Passes()
    {
        var city = $"  {new string('a', Location.Constraints.CityMaxLength)}  ";

        var result = await _validator.ValidateAsync(new GetEventsQuery { City = city });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_CityTooLongOrRangeReversed_ReportsEachField()
    {
        var query = new GetEventsQuery
        {
            City = new string('a', Location.Constraints.CityMaxLength + 1),
            DateFrom = new DateOnly(2027, 6, 2),
            DateTo = new DateOnly(2027, 6, 1)
        };

        var result = await _validator.ValidateAsync(query);

        Assert.Equal(["City", "DateTo"], result.Errors.Select(e => e.PropertyName).Order());
    }
}

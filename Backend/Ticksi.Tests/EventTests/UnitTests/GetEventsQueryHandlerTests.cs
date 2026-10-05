using Ticksi.Application.Common;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Events.Queries.GetEvents;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetEventsQueryHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_City_ReturnsOnlyEventsInThatCity()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var mostar = await AddVenueAsync("Mostar");
        await AddEventAsync(organizer, references, name: "Sarajevo Jazz");
        await AddEventAsync(organizer, references with { LocationId = mostar }, name: "Mostar Jazz");

        var page = await HandleAsync(new GetEventsQuery { City = "  Mostar " });

        Assert.Equal(["Mostar Jazz"], page.Items.Select(e => e.Name));
    }

    [Fact]
    public async Task Handle_DateRange_IncludesEveryEventOnItsFirstAndLastDay()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        await AddEventAsync(organizer, references, name: "Early", date: new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddEventAsync(organizer, references, name: "Late", date: new DateTime(2027, 6, 2, 23, 59, 59, DateTimeKind.Utc));
        await AddEventAsync(organizer, references, name: "Before", date: new DateTime(2027, 5, 31, 23, 59, 59, DateTimeKind.Utc));
        await AddEventAsync(organizer, references, name: "After", date: new DateTime(2027, 6, 3, 0, 0, 0, DateTimeKind.Utc));

        var page = await HandleAsync(new GetEventsQuery
        {
            DateFrom = new DateOnly(2027, 6, 1),
            DateTo = new DateOnly(2027, 6, 2)
        });

        Assert.Equal(["Early", "Late"], page.Items.Select(e => e.Name));
    }

    private async Task<Guid> AddVenueAsync(string city)
    {
        await using var context = Database.CreateContext();
        var venue = new Location { Name = $"{city} Arena", City = city, Address = "Bulevar 1", Capacity = 3000 };
        context.Locations.Add(venue);
        await context.SaveChangesAsync();
        return venue.PublicId;
    }

    private async Task<PagedResult<EventReadDto>> HandleAsync(GetEventsQuery query)
    {
        await using var context = Database.CreateContext();
        return await new GetEventsQueryHandler(context).Handle(query, CancellationToken.None);
    }
}

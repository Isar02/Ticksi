using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events.Queries.GetManagedEvents;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetManagedEventsQueryHandlerTests : EventHandlerTestBase
{
    private static readonly DateTimeOffset Today = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Organizer_SeesOnlyOwnEvents()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var other = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        await AddEventAsync(organizer, references, name: "Mine");
        await AddEventAsync(other, references, name: "Theirs");

        var result = await QueryAsync(organizer, new GetManagedEventsQuery());

        Assert.Equal(["Mine"], result.Items.Select(e => e.Name));
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Handle_Admin_SeesEveryEvent()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var references = await AddReferencesAsync();
        await AddEventAsync(await AddUserAsync(Role.Names.Organizer), references, name: "First");
        await AddEventAsync(await AddUserAsync(Role.Names.Organizer), references, name: "Second");

        var result = await QueryAsync(admin, new GetManagedEventsQuery { SortBy = "name" });

        Assert.Equal(["First", "Second"], result.Items.Select(e => e.Name));
    }

    [Fact]
    public async Task Handle_User_ThrowsForbidden()
    {
        var user = await AddUserAsync(Role.Names.User);

        await Assert.ThrowsAsync<ForbiddenException>(() => QueryAsync(user, new GetManagedEventsQuery()));
    }

    [Fact]
    public async Task Handle_NameCategoryAndVenueFilters_NarrowTheList()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var zetra = await AddReferencesAsync();
        var skenderija = await AddReferencesAsync();
        await AddEventAsync(organizer, zetra, name: "Jazz Night");
        await AddEventAsync(organizer, zetra, name: "Rock Night");
        await AddEventAsync(organizer, skenderija, name: "Jazz Brunch");

        var byName = await QueryAsync(organizer, new GetManagedEventsQuery { Name = " Jazz ", SortBy = "name" });
        var byCategory = await QueryAsync(organizer, new GetManagedEventsQuery { CategoryId = skenderija.CategoryId });
        var byVenue = await QueryAsync(organizer, new GetManagedEventsQuery { LocationId = zetra.LocationId, SortBy = "name" });

        Assert.Equal(["Jazz Brunch", "Jazz Night"], byName.Items.Select(e => e.Name));
        Assert.Equal(["Jazz Brunch"], byCategory.Items.Select(e => e.Name));
        Assert.Equal(["Jazz Night", "Rock Night"], byVenue.Items.Select(e => e.Name));
    }

    [Fact]
    public async Task Handle_DateRange_IncludesTheWholeLastDay()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        await AddEventAsync(organizer, references, name: "Before", date: new DateTime(2026, 11, 30, 23, 0, 0));
        await AddEventAsync(organizer, references, name: "First day", date: new DateTime(2026, 12, 1, 0, 0, 0));
        await AddEventAsync(organizer, references, name: "Last evening", date: new DateTime(2026, 12, 31, 22, 0, 0));
        await AddEventAsync(organizer, references, name: "After", date: new DateTime(2027, 1, 1, 0, 0, 0));

        var result = await QueryAsync(organizer, new GetManagedEventsQuery
        {
            DateFrom = new DateOnly(2026, 12, 1),
            DateTo = new DateOnly(2026, 12, 31)
        });

        Assert.Equal(["First day", "Last evening"], result.Items.Select(e => e.Name));
    }

    [Fact]
    public async Task Handle_LatestPossibleEndDate_KeepsEveryEvent()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        await AddEventAsync(organizer, await AddReferencesAsync());

        var result = await QueryAsync(organizer, new GetManagedEventsQuery { DateTo = DateOnly.MaxValue });

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Handle_Period_SplitsUpcomingAndPastAtNow()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        await AddEventAsync(organizer, references, name: "Yesterday", date: Today.UtcDateTime.AddDays(-1));
        await AddEventAsync(organizer, references, name: "Tomorrow", date: Today.UtcDateTime.AddDays(1));

        var upcoming = await QueryAsync(organizer, new GetManagedEventsQuery { Period = "Upcoming" });
        var past = await QueryAsync(organizer, new GetManagedEventsQuery { Period = "past" });

        Assert.Equal(["Tomorrow"], upcoming.Items.Select(e => e.Name));
        Assert.Equal(["Yesterday"], past.Items.Select(e => e.Name));
    }

    [Fact]
    public async Task Handle_Period_UsesTheEventTimeZone()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        await AddEventAsync(organizer, references, name: "Lunch", date: new DateTime(2026, 10, 5, 13, 0, 0));
        await AddEventAsync(organizer, references, name: "Dinner", date: new DateTime(2026, 10, 5, 19, 0, 0));
        _timeZone = "Europe/Sarajevo";

        var upcoming = await QueryAsync(organizer, new GetManagedEventsQuery { Period = "upcoming" });

        Assert.Equal(["Dinner"], upcoming.Items.Select(e => e.Name));
    }

    [Fact]
    public async Task Handle_SortBySold_CountsOnlyPaidOrders()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var references = await AddReferencesAsync();
        var quiet = await AddEventAsync(organizer, references, name: "Quiet");
        var popular = await AddEventAsync(organizer, references, name: "Popular");
        await AddOrderAsync(buyer, quiet.TicketTypes.First().PublicId, 2);
        await AddOrderAsync(buyer, quiet.TicketTypes.First().PublicId, 50, OrderStatus.Pending);
        await AddOrderAsync(buyer, popular.TicketTypes.First().PublicId, 3);
        await AddOrderAsync(buyer, popular.TicketTypes.Last().PublicId, 4);

        var result = await QueryAsync(organizer, new GetManagedEventsQuery { SortBy = "SOLD", SortDescending = true });

        Assert.Equal([("Popular", 7), ("Quiet", 2)], result.Items.Select(e => (e.Name, e.TicketsSold)));
        Assert.All(result.Items, e => Assert.Equal(600, e.TicketsTotal));
    }

    [Fact]
    public async Task Handle_Paging_ReturnsTheRequestedPage()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        foreach (var name in new[] { "A", "B", "C", "D", "E" })
            await AddEventAsync(organizer, references, name: name);

        var result = await QueryAsync(organizer, new GetManagedEventsQuery { SortBy = "name", Page = 2, PageSize = 2 });

        Assert.Equal(["C", "D"], result.Items.Select(e => e.Name));
        Assert.Equal((5, 3), (result.TotalCount, result.TotalPages));
    }

    private string _timeZone = "UTC";

    private async Task<PagedResult<ManagedEventDto>> QueryAsync(AppUser user, GetManagedEventsQuery query)
    {
        await using var context = Database.CreateContext();
        var handler = new GetManagedEventsQueryHandler(context, SignedIn(user), EventClocks.In(_timeZone, new FakeTimeProvider(Today)));
        return await handler.Handle(query, CancellationToken.None);
    }
}

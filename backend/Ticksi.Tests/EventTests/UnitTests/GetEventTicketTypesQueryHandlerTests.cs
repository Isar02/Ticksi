using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetEventTicketTypesQueryHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_ListsTheEventsTicketTypesWithTicketsLeft()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references, reserved: 120);
        await AddEventAsync(organizer, references, name: "Other");

        var ticketTypes = await GetAsync(item.PublicId);

        Assert.Equal(
            [("Standard", 20m, 380), ("VIP", 50m, 100)],
            ticketTypes.Select(t => (t.Name, t.Price, t.Available)));
        Assert.Equal(item.TicketTypes.Select(t => t.PublicId).Order(), ticketTypes.Select(t => t.PublicId).Order());
    }

    [Fact]
    public async Task Handle_UnknownEvent_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => GetAsync(Guid.NewGuid()));
    }

    private async Task<List<EventTicketTypeDto>> GetAsync(Guid eventId)
    {
        await using var context = Database.CreateContext();
        return await new GetEventTicketTypesQueryHandler(context)
            .Handle(new GetEventTicketTypesQuery(eventId), CancellationToken.None);
    }
}

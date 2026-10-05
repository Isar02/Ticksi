using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events.Commands.DeleteEvent;

namespace Ticksi.Tests.EventTests.UnitTests;

public class DeleteEventCommandHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_OwnEventWithoutOrders_DeletesIt()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(organizer, await AddReferencesAsync());

        await DeleteAsync(organizer, item.PublicId);

        Assert.Null(await FindEventAsync(item.PublicId));
    }

    [Fact]
    public async Task Handle_EventWithOrders_ThrowsConflictAndKeepsIt()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(organizer, await AddReferencesAsync());
        await AddOrderAsync(buyer, item.TicketTypes.First().PublicId, 1);

        await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(organizer, item.PublicId));

        Assert.NotNull(await FindEventAsync(item.PublicId));
    }

    [Fact]
    public async Task Handle_OtherOrganizersEvent_ThrowsForbidden()
    {
        var owner = await AddUserAsync(Role.Names.Organizer);
        var other = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(owner, await AddReferencesAsync());

        await Assert.ThrowsAsync<ForbiddenException>(() => DeleteAsync(other, item.PublicId));

        Assert.NotNull(await FindEventAsync(item.PublicId));
    }

    [Fact]
    public async Task Handle_Admin_DeletesAnyEvent()
    {
        var owner = await AddUserAsync(Role.Names.Organizer);
        var admin = await AddUserAsync(Role.Names.Admin);
        var item = await AddEventAsync(owner, await AddReferencesAsync());

        await DeleteAsync(admin, item.PublicId);

        Assert.Null(await FindEventAsync(item.PublicId));
    }

    private async Task DeleteAsync(AppUser user, Guid eventPublicId)
    {
        await using var context = Database.CreateContext();
        var handler = new DeleteEventCommandHandler(context, SignedIn(user));
        await handler.Handle(new DeleteEventCommand { PublicId = eventPublicId }, CancellationToken.None);
    }
}

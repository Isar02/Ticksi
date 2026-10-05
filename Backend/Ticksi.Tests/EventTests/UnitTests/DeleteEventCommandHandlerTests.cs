using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events.Commands.DeleteEvent;
using Ticksi.Tests.Common;

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
    public async Task Handle_EventWithPoster_RemovesThePosterFile()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(organizer, await AddReferencesAsync(), posterUrl: "/images/events/gala.jpg");
        Files.Add("/images/events/gala.jpg");

        await DeleteAsync(organizer, item.PublicId);

        Assert.Empty(Files.Files);
    }

    [Fact]
    public async Task Handle_PosterSharedWithAnotherEvent_KeepsThePosterFile()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var item = await AddEventAsync(organizer, references, posterUrl: "/images/events/shared.jpg");
        await AddEventAsync(organizer, references, name: "Encore", posterUrl: "/images/events/shared.jpg");
        Files.Add("/images/events/shared.jpg");

        await DeleteAsync(organizer, item.PublicId);

        Assert.Contains("/images/events/shared.jpg", Files.Files);
    }

    [Fact]
    public async Task Handle_PosterChangedAtTheSameTime_ThrowsConflictAndKeepsTheEventAndItsPoster()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var item = await AddEventAsync(organizer, await AddReferencesAsync(), posterUrl: "/images/events/old.png");
        Files.Add("/images/events/new.png");
        var parallelUpload = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Database.CreateContext();
            (await other.Events.SingleAsync(e => e.PublicId == item.PublicId, cancellationToken)).PosterUrl = "/images/events/new.png";
            await other.SaveChangesAsync(cancellationToken);
        });

        await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(organizer, item.PublicId, parallelUpload));

        Assert.NotNull(await FindEventAsync(item.PublicId));
        Assert.Equal(["/images/events/new.png"], Files.Files);
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

    private async Task DeleteAsync(AppUser user, Guid eventPublicId, params BeforeSaveInterceptor[] interceptors)
    {
        await using var context = Database.CreateContext(interceptors);
        var handler = new DeleteEventCommandHandler(context, SignedIn(user), Files);
        await handler.Handle(new DeleteEventCommand { PublicId = eventPublicId }, CancellationToken.None);
    }
}

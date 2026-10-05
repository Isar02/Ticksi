using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users.Commands.DeleteUser;

namespace Ticksi.Tests.UserTests.UnitTests;

public class DeleteUserCommandHandlerTests : UserHandlerTestBase
{
    [Fact]
    public async Task Handle_AccountWithoutHistory_IsDeletedWithItsCart()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.User);
        await using (var context = Database.CreateContext())
        {
            context.Carts.Add(new Cart { AppUserId = user.Id });
            await context.SaveChangesAsync();
        }

        await DeleteAsync(admin, user);

        Assert.Null(await FindUserAsync(user.PublicId));
        await using var check = Database.CreateContext();
        Assert.False(await check.Carts.AnyAsync(c => c.AppUserId == user.Id));
    }

    [Fact]
    public async Task Handle_AccountWithAnOrder_ThrowsConflictAndKeepsIt()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(organizer, await AddReferencesAsync());
        await AddOrderAsync(buyer, item.TicketTypes.First().PublicId, 2);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(admin, buyer));

        Assert.Contains("Deactivate it instead", exception.Message);
        Assert.NotNull(await FindUserAsync(buyer.PublicId));
    }

    [Fact]
    public async Task Handle_OrganizerWithEvents_ThrowsConflict()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        await AddEventAsync(organizer, await AddReferencesAsync());

        await Assert.ThrowsAsync<ConflictException>(() => DeleteAsync(admin, organizer));

        Assert.NotNull(await FindUserAsync(organizer.PublicId));
    }

    [Fact]
    public async Task Handle_OwnAccount_ThrowsForbidden()
    {
        var admin = await AddUserAsync(Role.Names.Admin);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() => DeleteAsync(admin, admin));

        Assert.Equal("You cannot delete your own account.", exception.Message);
        Assert.NotNull(await FindUserAsync(admin.PublicId));
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFound()
    {
        var admin = await AddUserAsync(Role.Names.Admin);

        await Assert.ThrowsAsync<NotFoundException>(() => DeleteAsync(admin, new AppUser()));
    }

    private async Task DeleteAsync(AppUser caller, AppUser user)
    {
        await using var context = Database.CreateContext();
        var handler = new DeleteUserCommandHandler(context, SignedIn(caller));
        await handler.Handle(new DeleteUserCommand { PublicId = user.PublicId }, CancellationToken.None);
    }
}

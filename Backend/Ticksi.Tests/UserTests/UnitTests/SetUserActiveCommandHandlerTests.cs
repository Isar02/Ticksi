using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users.Commands.SetUserActive;
using Ticksi.Domain.Enums;

namespace Ticksi.Tests.UserTests.UnitTests;

public class SetUserActiveCommandHandlerTests : UserHandlerTestBase
{
    [Fact]
    public async Task Handle_Deactivating_EndsTheSessionsAndActivatingAgainOpensNone()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.Organizer);
        await AddRefreshTokenAsync(user);

        await SetActiveAsync(admin, user, isActive: false);

        Assert.False((await FindUserAsync(user.PublicId))!.IsActive);
        Assert.Equal(RefreshTokenRevocation.AccountDeactivated, Assert.Single(await RefreshTokensOfAsync(user)).RevokedReason);

        await SetActiveAsync(admin, user, isActive: true);

        Assert.True((await FindUserAsync(user.PublicId))!.IsActive);
        Assert.NotNull(Assert.Single(await RefreshTokensOfAsync(user)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_DeactivatingAnInactiveAccount_RevokesNothingAgain()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.User, isActive: false);
        var token = await AddRefreshTokenAsync(user);

        await SetActiveAsync(admin, user, isActive: false);

        Assert.Null(Assert.Single(await RefreshTokensOfAsync(user)).RevokedAtUtc);
        Assert.Equal(token.Id, (await RefreshTokensOfAsync(user))[0].Id);
    }

    [Fact]
    public async Task Handle_OwnAccount_ThrowsForbidden()
    {
        var admin = await AddUserAsync(Role.Names.Admin);

        await Assert.ThrowsAsync<ForbiddenException>(() => SetActiveAsync(admin, admin, isActive: false));

        Assert.True((await FindUserAsync(admin.PublicId))!.IsActive);
    }

    [Fact]
    public async Task Handle_Organizer_ThrowsForbidden()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var user = await AddUserAsync(Role.Names.User);

        await Assert.ThrowsAsync<ForbiddenException>(() => SetActiveAsync(organizer, user, isActive: false));
    }

    private async Task SetActiveAsync(AppUser caller, AppUser user, bool isActive)
    {
        await using var context = Database.CreateContext();
        var handler = new SetUserActiveCommandHandler(context, SignedIn(caller), Clock);
        await handler.Handle(new SetUserActiveCommand { PublicId = user.PublicId, IsActive = isActive }, CancellationToken.None);
    }
}

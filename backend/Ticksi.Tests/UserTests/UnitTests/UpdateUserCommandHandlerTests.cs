using FluentValidation;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users.Commands.UpdateUser;
using Ticksi.Domain.Enums;

namespace Ticksi.Tests.UserTests.UnitTests;

public class UpdateUserCommandHandlerTests : UserHandlerTestBase
{
    [Fact]
    public async Task Handle_Admin_UpdatesTheFieldsAndTheRoleAndKeepsThePassword()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.User);
        var command = Command(user, OrganizerRoleId);

        await UpdateAsync(admin, command);

        var stored = (await FindUserAsync(user.PublicId))!;
        Assert.Equal("Amar", stored.FirstName);
        Assert.Equal(command.Email.Trim(), stored.Email);
        Assert.Equal("+387 62 555 444", stored.Phone);
        Assert.Equal(Role.Names.Organizer, stored.Role!.Name);
        Assert.Equal("hash", stored.PasswordHash);
    }

    [Fact]
    public async Task Handle_Deactivating_EndsEverySessionOfThatAccountOnly()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.User);
        await AddRefreshTokenAsync(user);
        await AddRefreshTokenAsync(user);
        await AddRefreshTokenAsync(user, RefreshTokenRevocation.SignedOut);
        var adminSession = await AddRefreshTokenAsync(admin);

        await UpdateAsync(admin, Command(user, UserRoleId, isActive: false));

        Assert.False((await FindUserAsync(user.PublicId))!.IsActive);
        var tokens = await RefreshTokensOfAsync(user);
        Assert.Equal(
            [RefreshTokenRevocation.AccountDeactivated, RefreshTokenRevocation.AccountDeactivated, RefreshTokenRevocation.SignedOut],
            tokens.Select(t => t.RevokedReason));
        Assert.Equal(Now, tokens[0].RevokedAtUtc);
        Assert.Null(Assert.Single(await RefreshTokensOfAsync(admin)).RevokedAtUtc);
        Assert.Equal(adminSession.Id, (await RefreshTokensOfAsync(admin))[0].Id);
    }

    [Fact]
    public async Task Handle_StayingActive_LeavesTheSessionsOpen()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.User);
        await AddRefreshTokenAsync(user);

        await UpdateAsync(admin, Command(user, OrganizerRoleId));

        Assert.Null(Assert.Single(await RefreshTokensOfAsync(user)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_OwnRoleOrOwnDeactivation_ThrowsForbiddenAndChangesNothing()
    {
        var admin = await AddUserAsync(Role.Names.Admin);

        var demote = await Assert.ThrowsAsync<ForbiddenException>(() => UpdateAsync(admin, Command(admin, UserRoleId)));
        var deactivate = await Assert.ThrowsAsync<ForbiddenException>(
            () => UpdateAsync(admin, Command(admin, AdminRoleId, isActive: false)));

        Assert.Equal("You cannot change your own role.", demote.Message);
        Assert.Equal("You cannot deactivate your own account.", deactivate.Message);
        var stored = (await FindUserAsync(admin.PublicId))!;
        Assert.Equal(Role.Names.Admin, stored.Role!.Name);
        Assert.True(stored.IsActive);
        Assert.Equal("Lejla", stored.FirstName);
    }

    [Fact]
    public async Task Handle_OwnDetails_AreSaved()
    {
        var admin = await AddUserAsync(Role.Names.Admin);

        await UpdateAsync(admin, Command(admin, AdminRoleId));

        Assert.Equal("Amar", (await FindUserAsync(admin.PublicId))!.FirstName);
    }

    [Fact]
    public async Task Handle_AnotherAccountsEmail_ThrowsAValidationErrorOnTheEmail()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.User);
        var command = Command(user, UserRoleId);
        command.Email = admin.Email;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => UpdateAsync(admin, command));

        Assert.Equal("Email", Assert.Single(exception.Errors).PropertyName);
    }

    [Fact]
    public async Task Handle_KeepingItsOwnEmail_IsNotATakenEmail()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var user = await AddUserAsync(Role.Names.User);
        var command = Command(user, UserRoleId);
        command.Email = user.Email;

        await UpdateAsync(admin, command);

        Assert.Equal(user.Email, (await FindUserAsync(user.PublicId))!.Email);
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFound()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var command = Command(admin, UserRoleId);
        command.PublicId = Guid.NewGuid();

        await Assert.ThrowsAsync<NotFoundException>(() => UpdateAsync(admin, command));
    }

    private static UpdateUserCommand Command(AppUser user, Guid roleId, bool isActive = true)
    {
        var command = UserInputOf<UpdateUserCommand>(roleId, isActive);
        command.PublicId = user.PublicId;
        return command;
    }

    private async Task UpdateAsync(AppUser caller, UpdateUserCommand command)
    {
        await using var context = Database.CreateContext();
        var handler = new UpdateUserCommandHandler(context, SignedIn(caller), Clock);
        await handler.Handle(command, CancellationToken.None);
    }
}

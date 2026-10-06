using FluentValidation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Profile.Commands.ChangePassword;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;
using Ticksi.Tests.AuthTests.UnitTests;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.ProfileTests.UnitTests;

public class ChangePasswordCommandHandlerTests : AuthHandlerTestBase
{
    private const string NewPassword = "Changed456";

    [Fact]
    public async Task Handle_CorrectCurrentPassword_StoresTheNewOneAndKeepsOnlyTheReturnedSession()
    {
        var user = await AddUserAsync();
        var thisDevice = await AddRefreshTokenAsync(user);
        var otherDevice = await AddRefreshTokenAsync(user);
        var otherUser = await AddUserAsync("other@ticksi.com");
        var otherUserSession = await AddRefreshTokenAsync(otherUser);

        var session = await HandleAsync(user, Password);

        var stored = await StoredUserAsync(user);
        Assert.NotEqual(PasswordCheck.Failed, PasswordHasher.Verify(stored, NewPassword));
        Assert.Equal(PasswordCheck.Failed, PasswordHasher.Verify(stored, Password));

        foreach (var ended in new[] { thisDevice, otherDevice })
        {
            var token = await RefreshTokenAsync(ended);
            Assert.Equal((RefreshTokenRevocation.PasswordChanged, Now), (token.RevokedReason, token.RevokedAtUtc));
        }

        Assert.Null((await RefreshTokenAsync(session.RefreshToken)).RevokedAtUtc);
        Assert.Equal(user.Email, session.Email);
        Assert.NotEmpty(session.AccessToken);
        Assert.Null((await RefreshTokenAsync(otherUserSession)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_FailsOnCurrentPasswordAndChangesNothing()
    {
        var user = await AddUserAsync();
        var token = await AddRefreshTokenAsync(user);

        var error = await Assert.ThrowsAsync<ValidationException>(() => HandleAsync(user, "Wrong123"));

        var failure = Assert.Single(error.Errors);
        Assert.Equal((nameof(ChangePasswordCommand.CurrentPassword), "The current password is incorrect."),
            (failure.PropertyName, failure.ErrorMessage));
        Assert.Equal(user.PasswordHash, (await StoredUserAsync(user)).PasswordHash);
        Assert.Single(await RefreshTokensOfAsync(user));
        Assert.Null((await RefreshTokenAsync(token)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_SessionRevokedByParallelRequest_StillChangesThePasswordAndEndsTheOthers()
    {
        var user = await AddUserAsync();
        var rotated = await AddRefreshTokenAsync(user);
        var otherDevice = await AddRefreshTokenAsync(user);

        var session = await HandleAsync(user, Password, RotatedByParallelRequest(rotated));

        Assert.NotEqual(PasswordCheck.Failed, PasswordHasher.Verify(await StoredUserAsync(user), NewPassword));
        Assert.Equal(RefreshTokenRevocation.Rotated, (await RefreshTokenAsync(rotated)).RevokedReason);
        Assert.Equal(RefreshTokenRevocation.PasswordChanged, (await RefreshTokenAsync(otherDevice)).RevokedReason);
        Assert.Null((await RefreshTokenAsync(session.RefreshToken)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ThrowsUnauthorized()
    {
        var user = await AddUserAsync();
        await DeactivateAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() => HandleAsync(user, Password));
    }

    [Fact]
    public async Task Handle_PasswordChangedAfterUserWasTracked_RejectsTheOldPasswordAndKeepsTheWinningSession()
    {
        var user = await AddUserAsync();
        await using var staleContext = Database.CreateContext();
        await staleContext.AppUsers.SingleAsync(u => u.Id == user.Id);

        var winningSession = await HandleAsync(user, Password);
        var staleHandler = new ChangePasswordCommandHandler(
            staleContext, new FakeCurrentUser(user.PublicId), TokenService, PasswordHasher, Clock);

        var error = await Assert.ThrowsAsync<ValidationException>(() => staleHandler.Handle(
            new ChangePasswordCommand { CurrentPassword = Password, NewPassword = "Overwritten789" }, CancellationToken.None));

        Assert.Equal(nameof(ChangePasswordCommand.CurrentPassword), Assert.Single(error.Errors).PropertyName);
        Assert.NotEqual(PasswordCheck.Failed, PasswordHasher.Verify(await StoredUserAsync(user), NewPassword));
        Assert.Null((await RefreshTokenAsync(winningSession.RefreshToken)).RevokedAtUtc);
        Assert.Single(await RefreshTokensOfAsync(user), t => t.RevokedAtUtc == null);
    }

    private async Task<AppUser> StoredUserAsync(AppUser user)
    {
        await using var context = Database.CreateContext();
        return await context.AppUsers.SingleAsync(u => u.Id == user.Id);
    }

    private async Task<AuthResponseDto> HandleAsync(AppUser user, string currentPassword, params IInterceptor[] interceptors)
    {
        await using var context = Database.CreateContext(interceptors);
        var handler = new ChangePasswordCommandHandler(context, new FakeCurrentUser(user.PublicId), TokenService, PasswordHasher, Clock);
        return await handler.Handle(new ChangePasswordCommand { CurrentPassword = currentPassword, NewPassword = NewPassword }, CancellationToken.None);
    }
}

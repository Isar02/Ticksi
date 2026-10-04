using Microsoft.EntityFrameworkCore.Diagnostics;
using Ticksi.Application.Features.Auth.Commands.Logout;
using Ticksi.Domain.Enums;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class LogoutCommandHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_ActiveToken_RevokesItAsSignedOutAndKeepsOtherSessions()
    {
        var user = await AddUserAsync();
        var token = await AddRefreshTokenAsync(user);
        var otherSession = await AddRefreshTokenAsync(user);

        await LogoutAsync(token);

        var signedOut = await RefreshTokenAsync(token);
        Assert.Equal(RefreshTokenRevocation.SignedOut, signedOut.RevokedReason);
        Assert.Equal(Now, signedOut.RevokedAtUtc);
        Assert.Null((await RefreshTokenAsync(otherSession)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_UnknownToken_ChangesNothing()
    {
        var user = await AddUserAsync();
        await AddRefreshTokenAsync(user);

        await LogoutAsync("not-a-stored-token");

        Assert.All(await RefreshTokensOfAsync(user), t => Assert.Null(t.RevokedAtUtc));
    }

    [Fact]
    public async Task Handle_TokenRevokedByParallelRequest_Completes()
    {
        var user = await AddUserAsync();
        var token = await AddRefreshTokenAsync(user);

        await LogoutAsync(token, RotatedByParallelRequest(token));

        Assert.Equal(RefreshTokenRevocation.Rotated, (await RefreshTokenAsync(token)).RevokedReason);
    }

    private async Task LogoutAsync(string refreshToken, params IInterceptor[] interceptors)
    {
        await using var context = Database.CreateContext(interceptors);
        var handler = new LogoutCommandHandler(context, TokenService, Clock);
        await handler.Handle(new LogoutCommand { RefreshToken = refreshToken }, CancellationToken.None);
    }
}

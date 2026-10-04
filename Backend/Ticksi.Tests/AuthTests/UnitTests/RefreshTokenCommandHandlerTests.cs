using Microsoft.EntityFrameworkCore.Diagnostics;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Auth.Commands.Refresh;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class RefreshTokenCommandHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_ActiveToken_RevokesItAsRotatedAndIssuesANewOne()
    {
        var user = await AddUserAsync();
        var oldToken = await AddRefreshTokenAsync(user);

        var response = await RefreshAsync(oldToken);

        Assert.NotEqual(oldToken, response.RefreshToken);
        Assert.Equal(user.Email, response.Email);

        var old = await RefreshTokenAsync(oldToken);
        Assert.Equal(RefreshTokenRevocation.Rotated, old.RevokedReason);
        Assert.Equal(Now, old.RevokedAtUtc);

        var issued = await RefreshTokenAsync(response.RefreshToken);
        Assert.Null(issued.RevokedAtUtc);
        Assert.Equal(user.Id, issued.AppUserId);
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsUnauthorized()
    {
        await AddUserAsync();

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync("not-a-stored-token"));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        var user = await AddUserAsync();
        var token = await AddRefreshTokenAsync(user);
        Clock.Advance(TimeSpan.FromDays(7));

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(token));

        Assert.Single(await RefreshTokensOfAsync(user));
    }

    [Fact]
    public async Task Handle_SignedOutToken_ThrowsUnauthorizedAndKeepsOtherSessions()
    {
        var user = await AddUserAsync();
        var signedOut = await AddRefreshTokenAsync(user, RefreshTokenRevocation.SignedOut);
        var otherSession = await AddRefreshTokenAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(signedOut));

        Assert.Null((await RefreshTokenAsync(otherSession)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_RotatedTokenReused_RevokesEverySessionOfThatUserOnly()
    {
        var user = await AddUserAsync();
        var reused = await AddRefreshTokenAsync(user, RefreshTokenRevocation.Rotated);
        await AddRefreshTokenAsync(user);
        await AddRefreshTokenAsync(user);
        var otherUser = await AddUserAsync("ivan@ticksi.com");
        var otherUserSession = await AddRefreshTokenAsync(otherUser);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(reused));

        var sessions = (await RefreshTokensOfAsync(user))
            .Where(t => t.RevokedReason != RefreshTokenRevocation.Rotated)
            .ToList();
        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, t => Assert.Equal(RefreshTokenRevocation.ReuseDetected, t.RevokedReason));
        Assert.Null((await RefreshTokenAsync(otherUserSession)).RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_TokenRotatedByParallelRequest_ThrowsUnauthorized()
    {
        var user = await AddUserAsync();
        var token = await AddRefreshTokenAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(token, RotatedByParallelRequest(token)));
    }

    [Fact]
    public async Task Handle_ReuseWhileOtherSessionsKeepRotating_StillRevokesEverySession()
    {
        const int parallelRotations = 5;
        var user = await AddUserAsync();
        var reused = await AddRefreshTokenAsync(user, RefreshTokenRevocation.Rotated);
        await AddRefreshTokenAsync(user);
        await AddRefreshTokenAsync(user);

        var keepsRotating = new BeforeSaveInterceptor(parallelRotations, async cancellationToken =>
        {
            await using var other = Database.CreateContext();
            var active = await other.RefreshTokens
                .FirstAsync(t => t.AppUserId == user.Id && t.RevokedAtUtc == null, cancellationToken);
            active.Revoke(RefreshTokenRevocation.Rotated, Now);
            other.RefreshTokens.Add(new RefreshToken
            {
                AppUserId = user.Id,
                TokenHash = TokenService.HashRefreshToken(Guid.NewGuid().ToString("N")),
                ExpiresAtUtc = Now.AddDays(7)
            });
            await other.SaveChangesAsync(cancellationToken);
        });

        await Assert.ThrowsAsync<UnauthorizedException>(() => RefreshAsync(reused, keepsRotating));

        var tokens = await RefreshTokensOfAsync(user);
        Assert.Equal(3 + parallelRotations, tokens.Count);
        Assert.All(tokens, t => Assert.NotNull(t.RevokedAtUtc));
    }

    private async Task<AuthResponseDto> RefreshAsync(string refreshToken, params IInterceptor[] interceptors)
    {
        await using var context = Database.CreateContext(interceptors);
        var handler = new RefreshTokenCommandHandler(context, TokenService, Clock);
        return await handler.Handle(new RefreshTokenCommand { RefreshToken = refreshToken }, CancellationToken.None);
    }
}

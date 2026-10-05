using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;
using Ticksi.Domain.Enums;
using Ticksi.Infrastructure.Security;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.AuthTests.UnitTests;

public abstract class AuthHandlerTestBase
{
    protected const string Password = "Secret123";

    protected readonly InMemoryDatabase Database = new();
    protected readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
    protected readonly IPasswordHasher PasswordHasher = new Pbkdf2PasswordHasher();
    protected readonly IJwtTokenService TokenService;

    protected AuthHandlerTestBase()
    {
        var jwt = new JwtOptions
        {
            Issuer = "Ticksi.Tests",
            Audience = "Ticksi.Tests",
            Key = "TestSigningKeyThatIsAtLeast32CharactersLong",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        };

        TokenService = new JwtTokenService(Options.Create(jwt), Clock);
    }

    protected DateTime Now => Clock.GetUtcNow().UtcDateTime;

    protected async Task<AppUser> AddUserAsync(string email = "ana@ticksi.com")
    {
        await using var context = Database.CreateContext();
        var role = await context.Roles.SingleAsync(r => r.Name == Role.Names.User);

        var user = new AppUser
        {
            FirstName = "Ana",
            LastName = "Kovac",
            Email = email,
            Phone = "+387 61 123 456",
            Role = role
        };
        user.PasswordHash = PasswordHasher.Hash(user, Password);

        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    protected async Task<string> AddRefreshTokenAsync(AppUser user, RefreshTokenRevocation? revokedAs = null)
    {
        var rawToken = Guid.NewGuid().ToString("N");
        var token = new RefreshToken
        {
            AppUserId = user.Id,
            TokenHash = TokenService.HashRefreshToken(rawToken),
            ExpiresAtUtc = Now.AddDays(7)
        };

        if (revokedAs is not null)
            token.Revoke(revokedAs.Value, Now);

        await using var context = Database.CreateContext();
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        return rawToken;
    }

    protected BeforeSaveInterceptor RotatedByParallelRequest(string rawToken)
    {
        var tokenHash = TokenService.HashRefreshToken(rawToken);

        return new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Database.CreateContext();
            var stored = await other.RefreshTokens.SingleAsync(t => t.TokenHash == tokenHash, cancellationToken);
            stored.Revoke(RefreshTokenRevocation.Rotated, Now);
            await other.SaveChangesAsync(cancellationToken);
        });
    }

    protected async Task DeactivateAsync(AppUser user)
    {
        await using var context = Database.CreateContext();
        (await context.AppUsers.SingleAsync(u => u.Id == user.Id)).IsActive = false;
        await context.SaveChangesAsync();
    }

    protected async Task<List<RefreshToken>> RefreshTokensOfAsync(AppUser user)
    {
        await using var context = Database.CreateContext();
        return await context.RefreshTokens.Where(t => t.AppUserId == user.Id).ToListAsync();
    }

    protected async Task<RefreshToken> RefreshTokenAsync(string rawToken)
    {
        var tokenHash = TokenService.HashRefreshToken(rawToken);
        await using var context = Database.CreateContext();
        return await context.RefreshTokens.SingleAsync(t => t.TokenHash == tokenHash);
    }
}

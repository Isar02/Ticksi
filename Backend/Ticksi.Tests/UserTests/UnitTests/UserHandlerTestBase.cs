using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Features.Users.Commands;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;
using Ticksi.Infrastructure.Security;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.UserTests.UnitTests;

public abstract class UserHandlerTestBase : EventHandlerTestBase
{
    protected static readonly Guid AdminRoleId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c01");
    protected static readonly Guid UserRoleId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c02");
    protected static readonly Guid OrganizerRoleId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c03");

    protected readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero));
    protected readonly IPasswordHasher PasswordHasher = new Pbkdf2PasswordHasher();

    protected DateTime Now => Clock.GetUtcNow().UtcDateTime;

    protected async Task<RefreshToken> AddRefreshTokenAsync(AppUser user, RefreshTokenRevocation? revokedAs = null)
    {
        var token = new RefreshToken
        {
            AppUserId = user.Id,
            TokenHash = Guid.NewGuid().ToString("N"),
            ExpiresAtUtc = Now.AddDays(7)
        };

        if (revokedAs is not null)
            token.Revoke(revokedAs.Value, Now.AddDays(-1));

        await using var context = Database.CreateContext();
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
        return token;
    }

    protected async Task<List<RefreshToken>> RefreshTokensOfAsync(AppUser user)
    {
        await using var context = Database.CreateContext();
        return await context.RefreshTokens.Where(t => t.AppUserId == user.Id).OrderBy(t => t.Id).ToListAsync();
    }

    protected async Task<AppUser?> FindUserAsync(Guid publicId)
    {
        await using var context = Database.CreateContext();
        return await context.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(u => u.PublicId == publicId);
    }

    protected static T UserInputOf<T>(Guid? roleId = null, bool isActive = true) where T : UserInput, new() => new()
    {
        FirstName = "  Amar  ",
        LastName = " Hadzic ",
        Email = $" amar.{Guid.NewGuid():N}@ticksi.com ",
        Phone = "+387 62 555 444",
        RoleId = roleId ?? OrganizerRoleId,
        IsActive = isActive
    };
}

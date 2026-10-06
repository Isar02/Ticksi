using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Auth;

internal static class UserSessions
{
    public static async Task EndAllAsync(
        IAppDbContext context,
        int userId,
        RefreshTokenRevocation reason,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var activeTokens = await context.RefreshTokens
                .Where(t => t.AppUserId == userId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
                token.Revoke(reason, nowUtc);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // A conflict is a row another request has just revoked; reloaded, it drops out of the next round.
                foreach (var entry in ex.Entries)
                    await entry.ReloadAsync(cancellationToken);
            }
        }
    }
}

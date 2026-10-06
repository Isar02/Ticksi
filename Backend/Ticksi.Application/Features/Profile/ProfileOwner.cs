using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Profile;

internal static class ProfileOwner
{
    public const string NotFoundMessage = "Your account could not be found.";

    public static IQueryable<AppUser> Query(IAppDbContext context, ICurrentUser currentUser)
    {
        var publicId = currentUser.RequirePublicId();
        return context.AppUsers.Where(u => u.PublicId == publicId && u.IsActive);
    }

    public static async Task<AppUser> LoadAsync(IAppDbContext context, ICurrentUser currentUser, CancellationToken cancellationToken) =>
        await Query(context, currentUser)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new UnauthorizedException(NotFoundMessage);
}

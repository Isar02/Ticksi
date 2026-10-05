using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Users;

internal sealed record UserAdministrator(int UserId)
{
    // Read from the database like the event editor, so a demoted or deactivated administrator loses access at once.
    public static async Task<UserAdministrator> ResolveAsync(
        IAppDbContext context,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var publicId = currentUser.RequirePublicId();

        var user = await context.AppUsers
            .Where(u => u.PublicId == publicId && u.IsActive)
            .Select(u => new { u.Id, Role = u.Role!.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedException("Your account could not be found.");

        if (user.Role != Role.Names.Admin)
            throw new ForbiddenException("Only administrators can manage user accounts.");

        return new UserAdministrator(user.Id);
    }

    public void EnsureNotSelf(AppUser user, string message)
    {
        if (user.Id == UserId)
            throw new ForbiddenException(message);
    }
}

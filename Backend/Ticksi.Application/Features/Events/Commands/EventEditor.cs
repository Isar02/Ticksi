using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Commands;

internal sealed record EventEditor(int UserId, bool IsAdmin)
{
    // The role is read from the database, so a changed or deactivated account loses access at once.
    public static async Task<EventEditor> ResolveAsync(
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

        return user.Role switch
        {
            Role.Names.Admin => new EventEditor(user.Id, IsAdmin: true),
            Role.Names.Organizer => new EventEditor(user.Id, IsAdmin: false),
            _ => throw new ForbiddenException("Only organizers and administrators can manage events.")
        };
    }

    public void EnsureCanManage(Event item)
    {
        if (!IsAdmin && item.AppUserId != UserId)
            throw new ForbiddenException("You can only manage your own events.");
    }
}

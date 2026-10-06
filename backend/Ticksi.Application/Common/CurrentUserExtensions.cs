using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Common;

public static class CurrentUserExtensions
{
    public static Guid RequirePublicId(this ICurrentUser currentUser) =>
        currentUser.PublicId ?? throw new UnauthorizedException("Your session is not valid. Please sign in again.");
}

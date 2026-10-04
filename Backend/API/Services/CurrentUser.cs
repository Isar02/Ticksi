using System.Security.Claims;
using Ticksi.Application.Interfaces;

namespace API.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? PublicId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var publicId)
            ? publicId
            : null;
}

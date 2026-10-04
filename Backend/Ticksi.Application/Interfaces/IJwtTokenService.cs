using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateAccessToken(AppUser user);
}

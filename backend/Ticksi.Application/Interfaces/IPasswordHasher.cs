using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(AppUser user, string password);
    PasswordCheck Verify(AppUser user, string password);
}

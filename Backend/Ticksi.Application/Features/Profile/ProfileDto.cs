using System.Linq.Expressions;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Profile;

public class ProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }

    public static readonly Expression<Func<AppUser, ProfileDto>> Projection = user => new ProfileDto
    {
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        Role = user.Role!.Name,
        RegistrationDate = user.RegistrationDate
    };

    private static readonly Func<AppUser, ProfileDto> FromUser = Projection.Compile();

    public static ProfileDto From(AppUser user) => FromUser(user);
}

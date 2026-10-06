using System.Linq.Expressions;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Users;

public class UserDto
{
    public Guid PublicId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime RegistrationDate { get; set; }

    public static readonly Expression<Func<AppUser, UserDto>> Projection = user => new UserDto
    {
        PublicId = user.PublicId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Phone = user.Phone,
        RoleId = user.Role!.PublicId,
        RoleName = user.Role.Name,
        IsActive = user.IsActive,
        RegistrationDate = user.RegistrationDate
    };

    private static readonly Func<AppUser, UserDto> FromUser = Projection.Compile();

    public static UserDto From(AppUser user) => FromUser(user);
}

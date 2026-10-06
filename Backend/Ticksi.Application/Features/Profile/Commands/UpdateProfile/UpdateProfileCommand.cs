using MediatR;

namespace Ticksi.Application.Features.Profile.Commands.UpdateProfile;

public class UpdateProfileCommand : IRequest<ProfileDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

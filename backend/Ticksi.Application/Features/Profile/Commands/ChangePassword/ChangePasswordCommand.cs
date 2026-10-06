using MediatR;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.Profile.Commands.ChangePassword;

public class ChangePasswordCommand : IRequest<AuthResponseDto>
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

using MediatR;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.Auth.Commands.Refresh;

public class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string RefreshToken { get; set; } = string.Empty;
}

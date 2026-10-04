using MediatR;

namespace Ticksi.Application.Features.Auth.Commands.Logout;

public class LogoutCommand : IRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

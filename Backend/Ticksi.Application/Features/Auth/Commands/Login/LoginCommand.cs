using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Models;

namespace Ticksi.Application.Features.Auth.Commands.Login
{
    public class LoginCommand : IRequest<ServiceResult<AuthResponseDto>>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}


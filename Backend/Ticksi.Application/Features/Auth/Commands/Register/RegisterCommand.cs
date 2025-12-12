using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Models;

namespace Ticksi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommand : IRequest<ServiceResult<AuthResponseDto>>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}


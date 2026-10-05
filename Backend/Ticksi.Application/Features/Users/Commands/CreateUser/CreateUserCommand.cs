using MediatR;

namespace Ticksi.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommand : UserInput, IRequest<UserDto>
{
    public string Password { get; set; } = string.Empty;
}

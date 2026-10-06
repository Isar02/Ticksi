using Ticksi.Application.Features.Auth;

namespace Ticksi.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : UserInputValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Password).NewPassword("Password");
    }
}

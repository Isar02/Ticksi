using FluentValidation;

namespace Ticksi.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : UserInputValidator<CreateUserCommand>
{
    private const int PasswordMinLength = 6;

    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordMinLength).WithMessage("Password must be at least {MinLength} characters.");
    }
}

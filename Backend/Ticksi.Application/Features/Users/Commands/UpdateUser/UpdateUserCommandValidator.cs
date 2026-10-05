using FluentValidation;

namespace Ticksi.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : UserInputValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty().WithMessage("User is required.");
    }
}

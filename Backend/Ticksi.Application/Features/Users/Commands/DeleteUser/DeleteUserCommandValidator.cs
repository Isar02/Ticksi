using FluentValidation;

namespace Ticksi.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty().WithMessage("User is required.");
    }
}

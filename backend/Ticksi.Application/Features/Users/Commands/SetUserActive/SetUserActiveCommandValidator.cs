using FluentValidation;

namespace Ticksi.Application.Features.Users.Commands.SetUserActive;

public class SetUserActiveCommandValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty().WithMessage("User is required.");
    }
}

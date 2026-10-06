using FluentValidation;
using Ticksi.Application.Features.Auth;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Users.Commands;

public abstract class UserInputValidator<T> : AbstractValidator<T> where T : UserInput
{
    protected UserInputValidator()
    {
        RuleFor(x => x.FirstName).PersonName("First name", AppUser.Constraints.FirstNameMaxLength);

        RuleFor(x => x.LastName).PersonName("Last name", AppUser.Constraints.LastNameMaxLength);

        RuleFor(x => x.Email).AccountEmail();

        RuleFor(x => x.Phone).PhoneNumber();

        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role is required.");
    }
}

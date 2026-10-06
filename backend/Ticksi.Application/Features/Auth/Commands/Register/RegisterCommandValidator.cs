using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).PersonName("First name", AppUser.Constraints.FirstNameMaxLength);

        RuleFor(x => x.LastName).PersonName("Last name", AppUser.Constraints.LastNameMaxLength);

        RuleFor(x => x.Email).AccountEmail();

        RuleFor(x => x.Password).NewPassword("Password");

        RuleFor(x => x.Phone).PhoneNumber();
    }
}

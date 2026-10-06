using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).PersonName("First name", AppUser.Constraints.FirstNameMaxLength);

        RuleFor(x => x.LastName).PersonName("Last name", AppUser.Constraints.LastNameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(AppUser.Constraints.EmailMaxLength)
            .WithMessage("Email cannot exceed {MaxLength} characters.");

        RuleFor(x => x.Password).NewPassword("Password");

        RuleFor(x => x.Phone).PhoneNumber(AppUser.Constraints.PhoneMaxLength);
    }
}

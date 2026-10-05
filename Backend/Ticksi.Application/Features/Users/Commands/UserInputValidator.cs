using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Users.Commands;

public abstract class UserInputValidator<T> : AbstractValidator<T> where T : UserInput
{
    private const int NameMinLength = 2;
    private const string PhonePattern = @"^\+?[0-9\s-]{9,}$";

    protected UserInputValidator()
    {
        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("First name is required.")
            .Must(value => value.Trim().Length >= NameMinLength).WithMessage($"First name must be at least {NameMinLength} characters.")
            .MaximumLength(AppUser.Constraints.FirstNameMaxLength).WithMessage("First name can have at most {MaxLength} characters.");

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Last name is required.")
            .Must(value => value.Trim().Length >= NameMinLength).WithMessage($"Last name must be at least {NameMinLength} characters.")
            .MaximumLength(AppUser.Constraints.LastNameMaxLength).WithMessage("Last name can have at most {MaxLength} characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(AppUser.Constraints.EmailMaxLength).WithMessage("Email can have at most {MaxLength} characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(PhonePattern).WithMessage("Enter a valid phone number.")
            .MaximumLength(AppUser.Constraints.PhoneMaxLength).WithMessage("Phone can have at most {MaxLength} characters.");

        RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role is required.");
    }
}

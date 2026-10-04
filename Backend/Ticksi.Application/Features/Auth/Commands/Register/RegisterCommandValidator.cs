using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        private const int NameMinLength = 2;
        private const int PasswordMinLength = 6;
        private const string PhonePattern = @"^\+?[0-9\s-]{9,}$";

        public RegisterCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MinimumLength(NameMinLength).WithMessage("First name must be at least {MinLength} characters.")
                .MaximumLength(AppUser.Constraints.FirstNameMaxLength)
                .WithMessage("First name cannot exceed {MaxLength} characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MinimumLength(NameMinLength).WithMessage("Last name must be at least {MinLength} characters.")
                .MaximumLength(AppUser.Constraints.LastNameMaxLength)
                .WithMessage("Last name cannot exceed {MaxLength} characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(AppUser.Constraints.EmailMaxLength)
                .WithMessage("Email cannot exceed {MaxLength} characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(PasswordMinLength).WithMessage("Password must be at least {MinLength} characters.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone is required.")
                .Matches(PhonePattern).WithMessage("Please enter a valid phone number.")
                .MaximumLength(AppUser.Constraints.PhoneMaxLength)
                .WithMessage("Phone cannot exceed {MaxLength} characters.");
        }
    }
}


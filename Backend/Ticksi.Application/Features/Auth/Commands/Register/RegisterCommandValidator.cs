using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(AppUser.Constraints.FirstNameMaxLength)
                .WithMessage("First name cannot exceed {MaxLength} characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(AppUser.Constraints.LastNameMaxLength)
                .WithMessage("Last name cannot exceed {MaxLength} characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(AppUser.Constraints.EmailMaxLength)
                .WithMessage("Email cannot exceed {MaxLength} characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone is required.")
                .MaximumLength(AppUser.Constraints.PhoneMaxLength)
                .WithMessage("Phone cannot exceed {MaxLength} characters.");
        }
    }
}


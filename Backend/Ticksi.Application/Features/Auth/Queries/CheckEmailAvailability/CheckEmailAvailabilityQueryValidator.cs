using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Queries.CheckEmailAvailability;

public class CheckEmailAvailabilityQueryValidator : AbstractValidator<CheckEmailAvailabilityQuery>
{
    public CheckEmailAvailabilityQueryValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(AppUser.Constraints.EmailMaxLength)
            .WithMessage("Email cannot exceed {MaxLength} characters.");
    }
}

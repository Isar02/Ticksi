using FluentValidation;

namespace Ticksi.Application.Features.Auth.Queries.CheckEmailAvailability;

public class CheckEmailAvailabilityQueryValidator : AbstractValidator<CheckEmailAvailabilityQuery>
{
    public CheckEmailAvailabilityQueryValidator()
    {
        RuleFor(x => x.Email).AccountEmail();
    }
}

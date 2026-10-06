using FluentValidation;

namespace Ticksi.Application.Features.Events.Queries.GetEventForEdit;

public class GetEventForEditQueryValidator : AbstractValidator<GetEventForEditQuery>
{
    public GetEventForEditQueryValidator()
    {
        RuleFor(x => x.EventId).NotEmpty().WithMessage("Event is required.");
    }
}

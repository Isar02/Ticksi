using FluentValidation;

namespace Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;

public class GetEventTicketTypesQueryValidator : AbstractValidator<GetEventTicketTypesQuery>
{
    public GetEventTicketTypesQueryValidator()
    {
        RuleFor(x => x.EventId).NotEmpty().WithMessage("Event is required.");
    }
}

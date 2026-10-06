using FluentValidation;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Commands.UpdateEvent;

public class UpdateEventCommandValidator : EventInputValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator(IEventClock eventClock) : base(eventClock)
    {
        RuleFor(x => x.PublicId).NotEmpty().WithMessage("Event is required.");
    }
}

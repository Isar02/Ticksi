using FluentValidation;

namespace Ticksi.Application.Features.Events.Commands.UpdateEvent;

public class UpdateEventCommandValidator : EventInputValidator<UpdateEventCommand>
{
    public UpdateEventCommandValidator(TimeProvider timeProvider) : base(timeProvider)
    {
        RuleFor(x => x.PublicId).NotEmpty().WithMessage("Event is required.");
    }
}

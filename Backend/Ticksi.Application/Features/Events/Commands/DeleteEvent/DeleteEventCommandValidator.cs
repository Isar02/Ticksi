using FluentValidation;

namespace Ticksi.Application.Features.Events.Commands.DeleteEvent;

public class DeleteEventCommandValidator : AbstractValidator<DeleteEventCommand>
{
    public DeleteEventCommandValidator()
    {
        RuleFor(x => x.PublicId).NotEmpty().WithMessage("Event is required.");
    }
}

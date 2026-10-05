namespace Ticksi.Application.Features.Events.Commands.CreateEvent;

public class CreateEventCommandValidator(TimeProvider timeProvider)
    : EventInputValidator<CreateEventCommand>(timeProvider);

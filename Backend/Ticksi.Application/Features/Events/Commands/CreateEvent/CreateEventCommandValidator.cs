using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Commands.CreateEvent;

public class CreateEventCommandValidator(IEventClock eventClock)
    : EventInputValidator<CreateEventCommand>(eventClock);

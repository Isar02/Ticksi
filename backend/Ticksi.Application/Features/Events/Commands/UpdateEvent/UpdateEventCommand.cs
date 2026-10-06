using MediatR;

namespace Ticksi.Application.Features.Events.Commands.UpdateEvent;

public class UpdateEventCommand : EventInput, IRequest
{
    public Guid PublicId { get; set; }
}

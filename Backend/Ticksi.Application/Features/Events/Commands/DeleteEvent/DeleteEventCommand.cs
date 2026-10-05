using MediatR;

namespace Ticksi.Application.Features.Events.Commands.DeleteEvent;

public class DeleteEventCommand : IRequest
{
    public Guid PublicId { get; set; }
}

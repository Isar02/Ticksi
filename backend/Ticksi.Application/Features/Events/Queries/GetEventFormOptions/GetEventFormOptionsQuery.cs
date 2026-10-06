using MediatR;

namespace Ticksi.Application.Features.Events.Queries.GetEventFormOptions;

public record GetEventFormOptionsQuery : IRequest<EventFormOptionsDto>;

using MediatR;

namespace Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;

public record GetEventTicketTypesQuery(Guid EventId) : IRequest<List<EventTicketTypeDto>>;

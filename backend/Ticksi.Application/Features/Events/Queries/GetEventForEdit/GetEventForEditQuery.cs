using MediatR;

namespace Ticksi.Application.Features.Events.Queries.GetEventForEdit;

public record GetEventForEditQuery(Guid EventId) : IRequest<EventForEditDto>;

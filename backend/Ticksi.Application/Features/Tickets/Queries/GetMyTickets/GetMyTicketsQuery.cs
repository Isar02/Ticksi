using MediatR;

namespace Ticksi.Application.Features.Tickets.Queries.GetMyTickets;

public record GetMyTicketsQuery : IRequest<List<TicketDto>>;

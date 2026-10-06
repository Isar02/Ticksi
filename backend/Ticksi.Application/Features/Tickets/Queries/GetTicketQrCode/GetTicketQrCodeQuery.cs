using MediatR;

namespace Ticksi.Application.Features.Tickets.Queries.GetTicketQrCode;

public record GetTicketQrCodeQuery(Guid TicketId) : IRequest<byte[]>;

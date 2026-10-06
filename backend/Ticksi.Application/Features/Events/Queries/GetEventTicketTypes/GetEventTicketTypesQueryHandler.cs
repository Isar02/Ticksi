using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;

public class GetEventTicketTypesQueryHandler : IRequestHandler<GetEventTicketTypesQuery, List<EventTicketTypeDto>>
{
    private readonly IAppDbContext _context;

    public GetEventTicketTypesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<EventTicketTypeDto>> Handle(GetEventTicketTypesQuery request, CancellationToken cancellationToken)
    {
        var ticketTypes = await _context.Events
            .AsNoTracking()
            .Where(e => e.PublicId == request.EventId)
            .Select(e => e.TicketTypes
                .OrderBy(t => t.Id)
                .Select(t => new EventTicketTypeDto
                {
                    PublicId = t.PublicId,
                    Name = t.Name,
                    Price = t.Price,
                    Available = t.Quantity - t.QuantityReserved
                })
                .ToList())
            .FirstOrDefaultAsync(cancellationToken);

        return ticketTypes ?? throw new NotFoundException("Event not found.");
    }
}

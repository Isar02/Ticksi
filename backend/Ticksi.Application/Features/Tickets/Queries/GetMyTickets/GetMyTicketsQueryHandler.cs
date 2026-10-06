using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Tickets.Queries.GetMyTickets;

public class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, List<TicketDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyTicketsQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<TicketDto>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        return await _context.Tickets
            .AsNoTracking()
            .OwnedBy(userPublicId)
            .OrderBy(t => t.OrderItem!.TicketType!.Event!.Date)
            .ThenBy(t => t.Id)
            .Select(TicketDto.Projection)
            .ToListAsync(cancellationToken);
    }
}

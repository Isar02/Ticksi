using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Queries.GetEventForEdit;

public class GetEventForEditQueryHandler : IRequestHandler<GetEventForEditQuery, EventForEditDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetEventForEditQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<EventForEditDto> Handle(GetEventForEditQuery request, CancellationToken cancellationToken)
    {
        var editor = await EventEditor.ResolveAsync(_context, _currentUser, cancellationToken);

        var found = await _context.Events
            .AsNoTracking()
            .Where(e => e.PublicId == request.EventId)
            .Select(e => new
            {
                OwnerId = e.AppUserId,
                Event = new EventForEditDto
                {
                    PublicId = e.PublicId,
                    Name = e.Name,
                    Description = e.Description,
                    Date = e.Date,
                    Contact = e.Contact,
                    PosterUrl = e.PosterUrl,
                    CategoryId = e.EventCategory!.PublicId,
                    CategoryName = e.EventCategory.Name,
                    EventTypeId = e.EventType!.PublicId,
                    LocationId = e.Location!.PublicId,
                    OrganizerCompanyId = e.OrganizerCompany!.PublicId,
                    TicketTypes = e.TicketTypes
                        .OrderBy(t => t.Id)
                        .Select(t => new TicketTypeForEditDto
                        {
                            PublicId = t.PublicId,
                            Name = t.Name,
                            Price = t.Price,
                            Quantity = t.Quantity,
                            QuantityReserved = t.QuantityReserved
                        })
                        .ToList()
                }
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        editor.EnsureCanManage(found.OwnerId);
        return found.Event;
    }
}

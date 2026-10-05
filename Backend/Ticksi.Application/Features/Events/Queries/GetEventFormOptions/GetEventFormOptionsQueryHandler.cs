using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Queries.GetEventFormOptions;

public class GetEventFormOptionsQueryHandler : IRequestHandler<GetEventFormOptionsQuery, EventFormOptionsDto>
{
    private readonly IAppDbContext _context;

    public GetEventFormOptionsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<EventFormOptionsDto> Handle(GetEventFormOptionsQuery request, CancellationToken cancellationToken)
    {
        return new EventFormOptionsDto
        {
            Venues = await _context.Locations
                .AsNoTracking()
                .OrderBy(l => l.Name)
                .Select(l => new VenueOptionDto(l.PublicId, l.Name, l.City, l.Capacity))
                .ToListAsync(cancellationToken),
            EventTypes = await _context.EventTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new OptionDto(t.PublicId, t.Name))
                .ToListAsync(cancellationToken),
            OrganizerCompanies = await _context.OrganizerCompanies
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new OptionDto(c.PublicId, c.Name))
                .ToListAsync(cancellationToken)
        };
    }
}

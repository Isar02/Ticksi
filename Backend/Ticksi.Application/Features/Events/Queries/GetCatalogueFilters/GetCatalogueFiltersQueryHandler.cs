using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Features.Events.Queries.GetEventFormOptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Queries.GetCatalogueFilters;

public class GetCatalogueFiltersQueryHandler : IRequestHandler<GetCatalogueFiltersQuery, CatalogueFiltersDto>
{
    private readonly IAppDbContext _context;

    public GetCatalogueFiltersQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<CatalogueFiltersDto> Handle(GetCatalogueFiltersQuery request, CancellationToken cancellationToken)
    {
        return new CatalogueFiltersDto
        {
            Categories = await _context.EventCategories
                .AsNoTracking()
                .Where(c => c.Events.Any())
                .OrderBy(c => c.Name)
                .Select(c => new OptionDto(c.PublicId, c.Name))
                .ToListAsync(cancellationToken),
            Cities = await _context.Locations
                .AsNoTracking()
                .Where(l => l.Events.Any())
                .Select(l => l.City)
                .Distinct()
                .OrderBy(city => city)
                .ToListAsync(cancellationToken)
        };
    }
}

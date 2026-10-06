using MediatR;

namespace Ticksi.Application.Features.Events.Queries.GetCatalogueFilters;

public record GetCatalogueFiltersQuery : IRequest<CatalogueFiltersDto>;

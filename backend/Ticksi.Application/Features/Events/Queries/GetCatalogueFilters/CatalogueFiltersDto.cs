using Ticksi.Application.Features.Events.Queries.GetEventFormOptions;

namespace Ticksi.Application.Features.Events.Queries.GetCatalogueFilters;

public class CatalogueFiltersDto
{
    public List<OptionDto> Categories { get; set; } = [];
    public List<string> Cities { get; set; } = [];
}

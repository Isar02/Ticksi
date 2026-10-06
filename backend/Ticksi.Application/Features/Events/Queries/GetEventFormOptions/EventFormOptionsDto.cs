namespace Ticksi.Application.Features.Events.Queries.GetEventFormOptions;

public class EventFormOptionsDto
{
    public List<OptionDto> Categories { get; set; } = [];
    public List<VenueOptionDto> Venues { get; set; } = [];
    public List<OptionDto> EventTypes { get; set; } = [];
    public List<OptionDto> OrganizerCompanies { get; set; } = [];
}

public record OptionDto(Guid PublicId, string Name);

public record VenueOptionDto(Guid PublicId, string Name, string City, int Capacity);

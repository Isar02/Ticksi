namespace Ticksi.Application.Features.Events.Queries.GetEventForEdit;

public class EventForEditDto
{
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Contact { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public Guid CategoryId { get; set; }
    public Guid EventTypeId { get; set; }
    public Guid LocationId { get; set; }
    public Guid OrganizerCompanyId { get; set; }
    public List<TicketTypeForEditDto> TicketTypes { get; set; } = [];
}

public class TicketTypeForEditDto
{
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int QuantityReserved { get; set; }
}

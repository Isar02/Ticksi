namespace Ticksi.Application.Features.Events.Commands;

public abstract class EventInput
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Contact { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid EventTypeId { get; set; }
    public Guid LocationId { get; set; }
    public Guid OrganizerCompanyId { get; set; }
    public List<TicketTypeInput> TicketTypes { get; set; } = [];
}

public class TicketTypeInput
{
    public Guid? PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

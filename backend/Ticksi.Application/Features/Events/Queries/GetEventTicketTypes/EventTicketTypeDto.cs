namespace Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;

public class EventTicketTypeDto
{
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Available { get; set; }
}

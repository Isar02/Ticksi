namespace Ticksi.Application.Features.Events.Queries.GetManagedEvents;

public class ManagedEventDto
{
    public Guid PublicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public int TicketsSold { get; set; }
    public int TicketsTotal { get; set; }
}

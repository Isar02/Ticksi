namespace Ticksi.Application.Features.Reports.Models;

public record EventSalesReport(
    string EventName,
    DateTime EventDate,
    string VenueName,
    string VenueCity,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<TicketTypeSales> TicketTypes,
    IReadOnlyList<DailySales> Days)
{
    public int TicketsSold => TicketTypes.Sum(t => t.TicketsSold);

    public decimal Revenue => TicketTypes.Sum(t => t.Revenue);
}

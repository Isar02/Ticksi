namespace Ticksi.Application.Features.Reports.Models;

public record DailySales(DateOnly Date, int TicketsSold, decimal Revenue);

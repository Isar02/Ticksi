using System.Text.Json.Serialization;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Dashboard;

public class DashboardDto
{
    public int UpcomingTickets { get; set; }
    public DashboardEventDto? NextEvent { get; set; }
    public int Favorites { get; set; }
    public List<DashboardOrderDto> RecentOrders { get; set; } = [];
    public DashboardSalesDto? Sales { get; set; }
}

public class DashboardEventDto
{
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string VenueCity { get; set; } = string.Empty;
    public int Tickets { get; set; }
}

public class DashboardOrderDto
{
    public Guid OrderId { get; set; }
    public List<string> EventNames { get; set; } = [];
    public int Tickets { get; set; }
    public decimal TotalAmount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class DashboardSalesDto
{
    public bool AllEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
    public int? ActiveUsers { get; set; }
    public List<DashboardTopEventDto> TopEvents { get; set; } = [];
}

public class DashboardTopEventDto
{
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
}

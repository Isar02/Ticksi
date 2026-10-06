using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Ticksi.Domain.Entities;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Tickets;

public class TicketDto
{
    public Guid PublicId { get; set; }
    public string Code { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TicketStatus Status { get; set; }

    public string TicketTypeName { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string VenueCity { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public DateTime IssuedAtUtc { get; set; }

    public static readonly Expression<Func<Ticket, TicketDto>> Projection = ticket => new TicketDto
    {
        PublicId = ticket.PublicId,
        Code = ticket.Code,
        Status = ticket.Status,
        TicketTypeName = ticket.OrderItem!.TicketType!.Name,
        EventId = ticket.OrderItem.TicketType.Event!.PublicId,
        EventName = ticket.OrderItem.TicketType.Event.Name,
        EventDate = ticket.OrderItem.TicketType.Event.Date,
        VenueName = ticket.OrderItem.TicketType.Event.Location!.Name,
        VenueCity = ticket.OrderItem.TicketType.Event.Location.City,
        OrderId = ticket.OrderItem.Order!.PublicId,
        IssuedAtUtc = ticket.IssuedAtUtc
    };
}

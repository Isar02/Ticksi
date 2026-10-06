using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Ticksi.Domain.Entities;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Orders;

public class OrderDto
{
    public Guid PublicId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];

    public static readonly Expression<Func<Order, OrderDto>> Projection = order => new OrderDto
    {
        PublicId = order.PublicId,
        Status = order.Status,
        TotalAmount = order.TotalAmount,
        CreatedAtUtc = order.CreatedAtUtc,
        Items = order.Items
            .OrderBy(i => i.Id)
            .Select(i => new OrderItemDto
            {
                EventId = i.TicketType!.Event!.PublicId,
                EventName = i.TicketType.Event.Name,
                EventDate = i.TicketType.Event.Date,
                TicketTypeName = i.TicketType.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            })
            .ToList()
    };
}

public class OrderItemDto
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

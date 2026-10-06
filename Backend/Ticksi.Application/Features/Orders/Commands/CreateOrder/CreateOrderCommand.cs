using MediatR;

namespace Ticksi.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<OrderDto>
{
    public List<OrderItemInput> Items { get; set; } = [];
}

public class OrderItemInput
{
    public Guid TicketTypeId { get; set; }
    public int Quantity { get; set; }
}

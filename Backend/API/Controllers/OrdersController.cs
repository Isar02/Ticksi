using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Orders;
using Ticksi.Application.Features.Orders.Commands.CreateOrder;
using Ticksi.Application.Features.Orders.Queries.GetOrderById;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetById(Guid orderId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetOrderByIdQuery(orderId), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { orderId = dto.PublicId }, dto);
    }
}

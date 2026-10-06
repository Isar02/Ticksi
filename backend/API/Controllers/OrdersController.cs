using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Orders;
using Ticksi.Application.Features.Orders.Commands.CreateOrder;
using Ticksi.Application.Features.Orders.Queries.GetOrderById;
using Ticksi.Application.Features.Payments;
using Ticksi.Application.Features.Payments.Commands.ConfirmPayment;
using Ticksi.Application.Features.Payments.Commands.StartPayment;

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

    [HttpPost("{orderId:guid}/payment")]
    [ProducesResponseType(typeof(PaymentSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentSessionDto>> StartPayment(Guid orderId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new StartPaymentCommand(orderId), cancellationToken));
    }

    [HttpPost("{orderId:guid}/payment/confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDto>> ConfirmPayment(Guid orderId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ConfirmPaymentCommand(orderId), cancellationToken));
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Tickets;
using Ticksi.Application.Features.Tickets.Queries.GetMyTickets;
using Ticksi.Application.Features.Tickets.Queries.GetTicketQrCode;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TicketDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TicketDto>>> GetMine(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetMyTicketsQuery(), cancellationToken));
    }

    [HttpGet("{ticketId:guid}/qr")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCode(Guid ticketId, CancellationToken cancellationToken)
    {
        var png = await _mediator.Send(new GetTicketQrCodeQuery(ticketId), cancellationToken);
        return File(png, "image/png");
    }
}

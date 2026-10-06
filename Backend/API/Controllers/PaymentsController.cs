using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Payments.Commands.HandlePaymentWebhook;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private const string SignatureHeader = "Stripe-Signature";

    private readonly IMediator _mediator;

    public PaymentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // The signature covers the exact bytes sent, so the body is read as text instead of being bound to a model.
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var command = new HandlePaymentWebhookCommand
        {
            Payload = await reader.ReadToEndAsync(cancellationToken),
            Signature = Request.Headers[SignatureHeader].ToString()
        };

        await _mediator.Send(command, cancellationToken);
        return Ok();
    }
}

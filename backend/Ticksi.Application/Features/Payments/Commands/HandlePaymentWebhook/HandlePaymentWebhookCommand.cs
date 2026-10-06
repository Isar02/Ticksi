using MediatR;

namespace Ticksi.Application.Features.Payments.Commands.HandlePaymentWebhook;

public class HandlePaymentWebhookCommand : IRequest
{
    public string Payload { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

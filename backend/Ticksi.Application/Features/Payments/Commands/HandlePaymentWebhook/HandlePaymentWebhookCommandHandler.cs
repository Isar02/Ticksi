using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Payments.Commands.HandlePaymentWebhook;

public class HandlePaymentWebhookCommandHandler : IRequestHandler<HandlePaymentWebhookCommand>
{
    private readonly IAppDbContext _context;
    private readonly IPaymentGateway _gateway;
    private readonly TimeProvider _timeProvider;

    public HandlePaymentWebhookCommandHandler(IAppDbContext context, IPaymentGateway gateway, TimeProvider timeProvider)
    {
        _context = context;
        _gateway = gateway;
        _timeProvider = timeProvider;
    }

    public async Task Handle(HandlePaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var received = _gateway.ReadWebhook(request.Payload, request.Signature)
            ?? throw new ValidationException([new ValidationFailure(nameof(request.Signature), "The webhook signature is not valid.")]);

        if (received.Kind == GatewayEventKind.Other ||
            received.Payment is not { Application: PaymentDefaults.Application } payment)
            return;

        await OrderSettlement.ApplyAsync(_context, payment, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }
}

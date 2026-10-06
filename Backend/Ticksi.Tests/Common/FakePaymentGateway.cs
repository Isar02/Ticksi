using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.Common;

// Stands in for the card processor: remembers the payments it created and lets a test decide how they end.
public sealed class FakePaymentGateway : IPaymentGateway
{
    public const string ValidSignature = "valid-signature";

    private readonly Dictionary<string, GatewayPayment> _byKey = [];
    private readonly Dictionary<string, GatewayPayment> _byReference = [];

    public List<PaymentRequest> Created { get; } = [];
    public Dictionary<string, GatewayEvent> Events { get; } = [];
    public bool Unavailable { get; set; }
    public int CreateCalls { get; private set; }

    public string PublishableKey => "pk_test_fake";

    public Task<GatewayPayment> CreateAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        CreateCalls++;

        if (_byKey.TryGetValue(request.IdempotencyKey, out var existing))
            return Task.FromResult(_byReference[existing.Reference]);

        Created.Add(request);
        var reference = $"pi_fake_{Created.Count}_{Guid.NewGuid():N}";
        var payment = new GatewayPayment(reference, $"{reference}_secret", GatewayPaymentState.Pending,
            request.Amount, request.Currency, request.Application, request.OrderId);

        _byKey[request.IdempotencyKey] = payment;
        _byReference[reference] = payment;
        return Task.FromResult(payment);
    }

    public Task<GatewayPayment> GetAsync(string reference, CancellationToken cancellationToken)
    {
        ThrowIfUnavailable();
        return Task.FromResult(_byReference[reference]);
    }

    public GatewayEvent? ReadWebhook(string payload, string signature) =>
        signature == ValidSignature ? Events.GetValueOrDefault(payload, new GatewayEvent(GatewayEventKind.Other, null)) : null;

    public GatewayPayment Report(string reference, GatewayPaymentState state, decimal? amount = null)
    {
        var payment = _byReference[reference] with { State = state, Amount = amount ?? _byReference[reference].Amount };
        _byReference[reference] = payment;
        return payment;
    }

    private void ThrowIfUnavailable()
    {
        if (Unavailable)
            throw new PaymentGatewayException("The card payment service is not available right now. Please try again.", new HttpRequestException());
    }
}

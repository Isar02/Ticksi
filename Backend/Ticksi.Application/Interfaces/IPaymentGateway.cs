namespace Ticksi.Application.Interfaces;

public interface IPaymentGateway
{
    string PublishableKey { get; }

    Task<GatewayPayment> CreateAsync(PaymentRequest request, CancellationToken cancellationToken);
    Task<GatewayPayment> GetAsync(string reference, CancellationToken cancellationToken);

    // Null when the signature does not match the payload.
    GatewayEvent? ReadWebhook(string payload, string signature);
}

public sealed record PaymentRequest(
    Guid OrderId,
    string Application,
    string Description,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

public sealed record GatewayPayment(
    string Reference,
    string ClientSecret,
    GatewayPaymentState State,
    decimal Amount,
    string Currency,
    string? Application,
    Guid? OrderId);

public sealed record GatewayEvent(GatewayEventKind Kind, GatewayPayment? Payment);

public enum GatewayPaymentState
{
    Pending,
    Succeeded,
    Failed,
    Canceled
}

public enum GatewayEventKind
{
    PaymentSucceeded,
    PaymentFailed,
    Other
}

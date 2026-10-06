using Microsoft.Extensions.Options;
using Stripe;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Infrastructure.Options;

namespace Ticksi.Infrastructure.Payments;

internal sealed class StripePaymentGateway : IPaymentGateway
{
    private const string ApplicationKey = "app";
    private const string OrderKey = "order_id";
    private const string StatementSuffix = "TICKSI";
    private const string Unavailable = "The card payment service is not available right now. Please try again.";

    private readonly StripeOptions _options;
    private readonly Lazy<PaymentIntentService> _intents;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        _intents = new(() => new PaymentIntentService(new StripeClient(Required(_options.SecretKey, nameof(StripeOptions.SecretKey)))));
    }

    public string PublishableKey => Required(_options.PublishableKey, nameof(StripeOptions.PublishableKey));

    public async Task<GatewayPayment> CreateAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = ToMinorUnits(request.Amount),
            Currency = request.Currency.ToLowerInvariant(),
            Description = request.Description,
            AllowedPaymentMethodTypes = ["card"],
            StatementDescriptorSuffix = StatementSuffix,
            Metadata = new Dictionary<string, string>
            {
                [ApplicationKey] = request.Application,
                [OrderKey] = request.OrderId.ToString()
            }
        };

        try
        {
            var intent = await _intents.Value.CreateAsync(options, new RequestOptions { IdempotencyKey = request.IdempotencyKey }, cancellationToken);
            return ToPayment(intent);
        }
        catch (StripeException e)
        {
            throw new PaymentGatewayException(Unavailable, e);
        }
    }

    public async Task<GatewayPayment> GetAsync(string reference, CancellationToken cancellationToken)
    {
        try
        {
            return ToPayment(await _intents.Value.GetAsync(reference, cancellationToken: cancellationToken));
        }
        catch (StripeException e)
        {
            throw new PaymentGatewayException(Unavailable, e);
        }
    }

    public GatewayEvent? ReadWebhook(string payload, string signature)
    {
        Event received;
        try
        {
            received = EventUtility.ConstructEvent(
                payload,
                signature,
                Required(_options.WebhookSecret, nameof(StripeOptions.WebhookSecret)),
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return null;
        }

        var kind = received.Type switch
        {
            EventTypes.PaymentIntentSucceeded => GatewayEventKind.PaymentSucceeded,
            EventTypes.PaymentIntentPaymentFailed => GatewayEventKind.PaymentFailed,
            _ => GatewayEventKind.Other
        };

        return new GatewayEvent(kind, received.Data.Object is PaymentIntent intent ? ToPayment(intent) : null);
    }

    private static GatewayPayment ToPayment(PaymentIntent intent) => new(
        intent.Id,
        intent.ClientSecret ?? string.Empty,
        StateOf(intent),
        intent.Amount / 100m,
        intent.Currency.ToUpperInvariant(),
        intent.Metadata.GetValueOrDefault(ApplicationKey),
        Guid.TryParse(intent.Metadata.GetValueOrDefault(OrderKey), out var orderId) ? orderId : null);

    private static GatewayPaymentState StateOf(PaymentIntent intent) => intent.Status switch
    {
        "succeeded" => GatewayPaymentState.Succeeded,
        "canceled" => GatewayPaymentState.Canceled,
        "requires_payment_method" when intent.LastPaymentError is not null => GatewayPaymentState.Failed,
        _ => GatewayPaymentState.Pending
    };

    // Two-decimal currencies such as BAM are charged in their smallest unit.
    private static long ToMinorUnits(decimal amount) => (long)decimal.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private static string Required(string value, string setting) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{StripeOptions.SectionName}:{setting} is not configured.")
            : value;
}

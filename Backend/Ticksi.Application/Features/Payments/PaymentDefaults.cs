namespace Ticksi.Application.Features.Payments;

public static class PaymentDefaults
{
    // Tags every payment, so events of other applications on the same card processor account are ignored.
    public const string Application = "ticksi";
    public const string Currency = "BAM";

    public static string OrderNumber(Guid orderId) => orderId.ToString("N")[..8].ToUpperInvariant();
}

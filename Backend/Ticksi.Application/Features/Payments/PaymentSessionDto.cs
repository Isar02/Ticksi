namespace Ticksi.Application.Features.Payments;

public class PaymentSessionDto
{
    public bool Paid { get; set; }
    public string? ClientSecret { get; set; }
    public string? PublishableKey { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = PaymentDefaults.Currency;
}

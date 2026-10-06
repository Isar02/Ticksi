using Ticksi.Domain.Enums;

namespace Ticksi.Domain.Entities;

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public string ProviderReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public static class Constraints
    {
        public const int ProviderReferenceMaxLength = 255;
        public const int CurrencyLength = 3;
    }
}

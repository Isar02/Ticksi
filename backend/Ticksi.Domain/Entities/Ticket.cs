using Ticksi.Domain.Enums;

namespace Ticksi.Domain.Entities;

public class Ticket : BaseEntity
{
    public int OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }

    public string Code { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Valid;
    public DateTime IssuedAtUtc { get; set; }

    public static class Constraints
    {
        public const int CodeMaxLength = 32;
    }
}

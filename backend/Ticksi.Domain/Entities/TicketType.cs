namespace Ticksi.Domain.Entities;

public class TicketType : BaseEntity
{
    public int EventId { get; set; }
    public Event? Event { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    // Held by pending and paid orders; the rest is still for sale.
    public int QuantityReserved { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public static class Constraints
    {
        public const int NameMaxLength = 100;
    }
}

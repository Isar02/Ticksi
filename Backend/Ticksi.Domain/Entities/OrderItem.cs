namespace Ticksi.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int TicketTypeId { get; set; }
    public TicketType? TicketType { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public static class Constraints
    {
        public const int MaxQuantity = 10;
    }
}

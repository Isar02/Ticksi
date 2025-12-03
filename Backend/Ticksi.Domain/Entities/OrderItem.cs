namespace Ticksi.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public decimal Price { get; set; } // Snapshot price at purchase
    }
}
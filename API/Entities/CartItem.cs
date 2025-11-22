namespace API.Entities
{
    public class CartItem : BaseEntity
    {
        public int CartId { get; set; }
        public Cart? Cart { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public decimal Price { get; set; } // Snapshot price when added
    }
}
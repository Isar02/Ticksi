namespace API.Entities
{
    public class Ticket : BaseEntity
    {
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int UserId { get; set; } // Buyer
        public User? User { get; set; }

        public int? SeatId { get; set; } // Nullable for standing tickets
        public Seat? Seat { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string QrCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = "Reserved";

        // Navigation
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
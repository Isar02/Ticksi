namespace API.Entities
{
    public class Order : BaseEntity
    {
        public int CartId { get; set; }
        public Cart? Cart { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public string OrderStatus { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
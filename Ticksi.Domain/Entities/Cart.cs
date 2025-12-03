namespace Ticksi.Domain.Entities
{
    public class Cart : BaseEntity
    {
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
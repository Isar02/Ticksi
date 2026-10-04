namespace Ticksi.Domain.Entities;

public class Cart : BaseEntity
{
    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

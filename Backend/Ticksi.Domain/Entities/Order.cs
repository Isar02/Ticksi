using Ticksi.Domain.Enums;

namespace Ticksi.Domain.Entities;

public class Order : BaseEntity
{
    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

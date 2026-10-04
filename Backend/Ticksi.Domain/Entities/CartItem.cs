namespace Ticksi.Domain.Entities;

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public Cart? Cart { get; set; }

    public int TicketTypeId { get; set; }
    public TicketType? TicketType { get; set; }

    public int Quantity { get; set; }
    public bool IsSavedForLater { get; set; }
}

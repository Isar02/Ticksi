namespace Ticksi.Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string TransactionStatus { get; set; } = "Pending";

        public int? PromoCodeId { get; set; } // Optional
        public PromoCode? PromoCode { get; set; }

        // Navigation
        public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }
}
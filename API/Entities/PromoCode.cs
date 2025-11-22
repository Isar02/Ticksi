namespace API.Entities
{
    public class PromoCode : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public decimal Discount { get; set; } // percentage or fixed amount
        public bool IsActive { get; set; } = true;
        public DateTime ExpirationDate { get; set; }

        // Navigation
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
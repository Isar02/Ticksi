namespace API.Entities
{
    public class Refund : BaseEntity
    {
        public int TransactionId { get; set; }
        public Transaction? Transaction { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Requested";
    }
}
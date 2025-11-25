namespace API.Entities
{
    public class Review : BaseEntity
    {
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
namespace Ticksi.Domain.Entities
{
    public class Favorite : BaseEntity
    {
        // Foreign keys
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        public int EventId { get; set; }
        public Event? Event { get; set; }
    }
}


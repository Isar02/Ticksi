namespace Ticksi.Domain.Entities
{
    public class Event : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Contact { get; set; } = string.Empty;
        public string? PosterUrl { get; set; }

        // Foreign keys
        public int AppUserId { get; set; } // Creator
        public AppUser? AppUser { get; set; }

        public int OrganizerCompanyId { get; set; }
        public OrganizerCompany? OrganizerCompany { get; set; }

        public int EventTypeId { get; set; }
        public EventType? EventType { get; set; }

        public int EventCategoryId { get; set; }
        public EventCategory? EventCategory { get; set; }

        public int LocationId { get; set; }
        public Location? Location { get; set; }

        // Navigation
        public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}

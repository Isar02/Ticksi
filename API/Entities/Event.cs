namespace API.Entities
{
    public class Event : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public int TicketCount { get; set; }
        public string Contact { get; set; } = string.Empty;

        // Foreign keys
        public int UserId { get; set; } // Creator
        public User? User { get; set; }

        public int OrganizerCompanyId { get; set; }
        public OrganizerCompany? OrganizerCompany { get; set; }

        public int EventTypeId { get; set; }
        public EventType? EventType { get; set; }

        public int EventCategoryId { get; set; }
        public EventCategory? EventCategory { get; set; }

        public int LocationId { get; set; }
        public Location? Location { get; set; }

        // Navigation
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
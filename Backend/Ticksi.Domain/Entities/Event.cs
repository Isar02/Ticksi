namespace Ticksi.Domain.Entities;

public class Event : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Contact { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }

    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public int OrganizerCompanyId { get; set; }
    public OrganizerCompany? OrganizerCompany { get; set; }

    public int EventTypeId { get; set; }
    public EventType? EventType { get; set; }

    public int EventCategoryId { get; set; }
    public EventCategory? EventCategory { get; set; }

    public int LocationId { get; set; }
    public Location? Location { get; set; }

    public ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public static class Constraints
    {
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 4000;
        public const int ContactMaxLength = 200;
        public const int PosterUrlMaxLength = 500;
    }
}

namespace API.Entities;

public class EventCategory : BaseEntity
{
    
    
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

        // Future: Navigation to Events in this category
        public ICollection<Event> Events { get; set; } = new List<Event>();
}
namespace API.Entities;

public class EventType : BaseEntity
{
    
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

        // Future: Navigation to Events of this type
        public ICollection<Event> Events { get; set; } = new List<Event>();
}
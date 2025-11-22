namespace API.Entities;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Capacity { get; set; }

        // Navigation
        public ICollection<Seat> Seats { get; set; } = new List<Seat>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
    
}
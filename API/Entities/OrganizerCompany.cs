namespace API.Entities;

public class OrganizerCompany : BaseEntity
{
        
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;

        // Future: Navigation to Events they organize
        public ICollection<Event> Events { get; set; } = new List<Event>();
}
namespace Ticksi.Application.DTOs
{
    public class EventReadDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public int TicketCount { get; set; }
        public string Contact { get; set; } = string.Empty;

        // Related entities
        public string EventCategoryName { get; set; } = string.Empty;
        public Guid EventCategoryPublicId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string EventTypeName { get; set; } = string.Empty;
        public string OrganizerCompanyName { get; set; } = string.Empty;
    }
}


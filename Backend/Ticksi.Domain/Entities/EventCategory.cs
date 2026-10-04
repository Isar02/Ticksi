namespace Ticksi.Domain.Entities;

public class EventCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string PosterUrl { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();

    public static class Constraints
    {
        public const int NameMaxLength = 100;
        public const int DescriptionMaxLength = 500;
        public const int PosterUrlMaxLength = 500;
    }
}

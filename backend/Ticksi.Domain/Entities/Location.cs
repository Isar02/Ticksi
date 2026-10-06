namespace Ticksi.Domain.Entities;

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();

    public static class Constraints
    {
        public const int NameMaxLength = 200;
        public const int CityMaxLength = 100;
        public const int AddressMaxLength = 300;
    }
}

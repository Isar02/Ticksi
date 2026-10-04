namespace Ticksi.Domain.Entities;

public class OrganizerCompany : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();

    public static class Constraints
    {
        public const int NameMaxLength = 200;
        public const int EmailMaxLength = 256;
        public const int PhoneMaxLength = 20;
        public const int AddressMaxLength = 300;
        public const int TaxIdMaxLength = 20;
    }
}

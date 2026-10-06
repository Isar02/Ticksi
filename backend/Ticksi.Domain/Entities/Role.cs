namespace Ticksi.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();

    public static class Names
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Organizer = "Organizer";
    }

    public static class Constraints
    {
        public const int NameMaxLength = 50;
    }
}

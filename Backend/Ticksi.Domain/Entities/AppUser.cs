namespace Ticksi.Domain.Entities;

public class AppUser : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public static class Constraints
    {
        public const int FirstNameMaxLength = 100;
        public const int LastNameMaxLength = 100;
        public const int EmailMaxLength = 256;
        public const int PasswordHashMaxLength = 256;
        public const int PhoneMaxLength = 20;
    }
}

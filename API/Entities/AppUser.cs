namespace API.Entities;

public class AppUser : BaseEntity
{
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
}

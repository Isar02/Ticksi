namespace Ticksi.Domain.Entities;

    public class Role : BaseEntity
    {
       
        public string Name { get; set; } = string.Empty;

        public ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();
    }

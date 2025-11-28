namespace API.Entities;

public class AppUser : BaseEntity
{
        
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active";

        // Foreign key
        public int RoleId { get; set; }
        public Role? Role { get; set; } // Navigation property

        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();


}

namespace API.Entities
{
    public class Notification : BaseEntity
    {
        public int UserId { get; set; }
        public User? User { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
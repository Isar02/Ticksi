namespace Ticksi.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public int AppUserId { get; set; }
        public AppUser? AppUser { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
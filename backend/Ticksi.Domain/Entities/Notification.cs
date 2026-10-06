namespace Ticksi.Domain.Entities;

public class Notification : BaseEntity
{
    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public static class Constraints
    {
        public const int TitleMaxLength = 200;
        public const int MessageMaxLength = 1000;
    }
}

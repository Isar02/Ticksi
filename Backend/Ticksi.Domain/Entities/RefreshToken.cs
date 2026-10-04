using Ticksi.Domain.Enums;

namespace Ticksi.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public RefreshTokenRevocation? RevokedReason { get; set; }

    public void Revoke(RefreshTokenRevocation reason, DateTime nowUtc)
    {
        RevokedAtUtc = nowUtc;
        RevokedReason = reason;
    }

    public static class Constraints
    {
        public const int TokenHashMaxLength = 64;
    }
}

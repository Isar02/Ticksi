namespace Ticksi.Domain.Enums;

public enum RefreshTokenRevocation
{
    Rotated = 1,
    SignedOut = 2,
    ReuseDetected = 3,
    AccountDeactivated = 4,
    PasswordChanged = 5
}

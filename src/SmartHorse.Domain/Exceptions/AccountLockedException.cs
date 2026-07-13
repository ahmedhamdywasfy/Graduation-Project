namespace SmartHorse.Domain.Exceptions;

/// <summary>
/// Thrown when a login attempt is made against an account that has exceeded the
/// allowed number of failed attempts and is temporarily locked (v0.2 Security
/// Review, Section 8 — account lockout). Mapped to HTTP 423 (Locked).
/// </summary>
public class AccountLockedException : DomainException
{
    public AccountLockedException(DateTime lockedUntilUtc)
        : base($"Account is locked until {lockedUntilUtc:u} due to repeated failed login attempts.")
    {
        LockedUntilUtc = lockedUntilUtc;
    }

    public DateTime LockedUntilUtc { get; }
}

namespace SmartHorse.Domain.Enums;

/// <summary>
/// Audit event types tracked by the User Audit Logs feature (Sprint 2 §6).
/// Deliberately a closed enum (rather than a free-text string) so audit queries
/// can filter reliably and the seeded permission "audit.view" has a well-defined
/// scope of events it grants visibility into.
/// </summary>
public enum AuditAction
{
    Register = 1,
    Login = 2,
    LoginFailed = 3,
    Logout = 4,
    EmailConfirmationRequested = 5,
    EmailConfirmed = 6,
    ForgotPasswordRequested = 7,
    PasswordReset = 8,
    PasswordChanged = 9,
    RefreshTokenUsed = 10,
    RefreshTokenReuseDetected = 11,
    ProfileUpdated = 12,
    AvatarUpdated = 13,
    AccountLockedOut = 14,
    AccountUnlockedByAdministrator = 15,
    AccountDeactivatedByAdministrator = 16
}

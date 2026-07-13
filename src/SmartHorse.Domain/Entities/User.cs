using SmartHorse.Domain.Common;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// Core account aggregate root (v0.1 Section 13). Covers every role — Owner,
/// Veterinarian, Trainer, Worker, Buyer, Administrator — via the <see cref="UserRoles"/>
/// collection rather than a single fixed Role column, since v0.1 explicitly allows a
/// user to hold more than one role.
///
/// Business invariants (password format, lockout thresholds, etc.) that require
/// external services (hashing) are deliberately kept out of this entity and live in
/// the Application layer command handlers, which call <see cref="Common.Interfaces.IPasswordHasher"/>.
/// What belongs here is state transition logic that depends only on the entity's own
/// data (Deactivate, RecordFailedLogin, etc.) per Clean Architecture / DDD conventions.
/// </summary>
public class User : BaseAuditableEntity
{
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<UserRole> _userRoles = new();
    private readonly List<RefreshToken> _refreshTokens = new();
    private readonly List<UserPermissionOverride> _permissionOverrides = new();

    private User()
    {
        // Required by EF Core.
        FullName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string fullName, string email, string passwordHash, string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        PhoneNumber = phoneNumber?.Trim();
        IsActive = true;
    }

    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string? PhoneNumber { get; private set; }
    public bool IsActive { get; private set; }

    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }

    public bool EmailConfirmed { get; private set; }
    public string? EmailConfirmationTokenHash { get; private set; }
    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; private set; }

    public string? AvatarUrl { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<UserPermissionOverride> PermissionOverrides => _permissionOverrides.AsReadOnly();

    public bool IsLockedOut => LockedUntilUtc.HasValue && LockedUntilUtc.Value > DateTime.UtcNow;

    public void AssignRole(Role role)
    {
        if (_userRoles.Any(ur => ur.RoleId == role.Id))
        {
            return; // idempotent
        }

        _userRoles.Add(new UserRole(Id, role.Id));
    }

    public void ReplaceRoles(IEnumerable<Role> roles)
    {
        _userRoles.Clear();
        foreach (var role in roles)
        {
            _userRoles.Add(new UserRole(Id, role.Id));
        }
    }

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.", nameof(fullName));
        }

        FullName = fullName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));
        }

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new AccountInactiveException();
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Called by the Login command handler on a failed password check. Applies the
    /// lockout policy described in v0.2 Security Review, Section 8.
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockedUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
        }
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
    }

    /// <summary>
    /// Administrator-triggered unlock (v0.2 §5 "Administrator Unlock Support"),
    /// distinct from the automatic unlock that happens once <see cref="LockedUntilUtc"/>
    /// passes on its own.
    /// </summary>
    public void UnlockByAdministrator()
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAtUtc, string? createdByIp = null, string? userAgent = null)
    {
        var token = new RefreshToken(Id, tokenHash, expiresAtUtc, createdByIp, userAgent);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>
    /// Issues a one-time password-reset token hash (v0.1 Section 12 — Forgot
    /// Password). The raw token is emailed to the user and never persisted;
    /// only its hash is stored here for later verification in ResetPassword.
    /// </summary>
    public void SetPasswordResetToken(string tokenHash, DateTime expiresAtUtc)
    {
        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAtUtc = expiresAtUtc;
    }

    public bool IsPasswordResetTokenValid(string presentedTokenHash)
    {
        return PasswordResetTokenHash is not null
            && PasswordResetTokenExpiresAtUtc.HasValue
            && PasswordResetTokenExpiresAtUtc.Value > DateTime.UtcNow
            && PasswordResetTokenHash == presentedTokenHash;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAtUtc = null;
    }

    /// <summary>
    /// Issues a one-time email confirmation token hash (Sprint 2 — Email
    /// Confirmation). Used both for the initial post-registration confirmation
    /// and for "resend confirmation email" (which simply re-issues a fresh token).
    /// </summary>
    public void SetEmailConfirmationToken(string tokenHash, DateTime expiresAtUtc)
    {
        if (EmailConfirmed)
        {
            throw new EmailAlreadyConfirmedException();
        }

        EmailConfirmationTokenHash = tokenHash;
        EmailConfirmationTokenExpiresAtUtc = expiresAtUtc;
    }

    public bool IsEmailConfirmationTokenValid(string presentedTokenHash)
    {
        return !EmailConfirmed
            && EmailConfirmationTokenHash is not null
            && EmailConfirmationTokenExpiresAtUtc.HasValue
            && EmailConfirmationTokenExpiresAtUtc.Value > DateTime.UtcNow
            && EmailConfirmationTokenHash == presentedTokenHash;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        EmailConfirmationTokenHash = null;
        EmailConfirmationTokenExpiresAtUtc = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvatarUrl(string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            throw new ArgumentException("Avatar URL cannot be empty.", nameof(avatarUrl));
        }

        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}

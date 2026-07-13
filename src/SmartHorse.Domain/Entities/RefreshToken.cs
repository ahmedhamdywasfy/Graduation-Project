using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// A single issued refresh token for a user (v0.2 Security Review, Section 8).
/// Tokens are stored hashed (never in plain text), rotate on every use, and
/// support reuse detection: if a token whose <see cref="ReplacedByTokenId"/> is
/// already set is presented again, the whole chain is revoked as a theft signal.
/// </summary>
public class RefreshToken : BaseEntity
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, string? createdByIp = null, string? userAgent = null)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
        IsRevoked = false;
        CreatedByIp = createdByIp;
        UserAgent = userAgent;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    /// <summary>SHA-256 hash of the token value; the raw value is never persisted.</summary>
    public string TokenHash { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    /// <summary>Set when this token is rotated out in favor of a newer one.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>IP address the token was issued to (Sprint 2 §4 — "Store Tokens Securely" / session review).</summary>
    public string? CreatedByIp { get; private set; }

    /// <summary>Raw User-Agent header the token was issued to.</summary>
    public string? UserAgent { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke()
    {
        IsRevoked = true;
    }

    public void MarkReplacedBy(Guid newTokenId)
    {
        ReplacedByTokenId = newTokenId;
        IsRevoked = true;
    }
}

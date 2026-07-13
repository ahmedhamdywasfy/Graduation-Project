using SmartHorse.Domain.Common;
using SmartHorse.Domain.Enums;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// Append-only audit trail entry (Sprint 2 §6 — User Audit Logs; also referenced
/// by v0.1 §13 AuditLogs and v0.2 §2.1 Audit Logs sub-module). Written once and
/// never updated — there are intentionally no setters beyond the constructor.
/// </summary>
public class AuditLog : BaseEntity
{
    private AuditLog()
    {
        IpAddress = string.Empty;
        UserAgent = string.Empty;
    }

    public AuditLog(Guid? userId, AuditAction action, string ipAddress, string userAgent, string? details = null)
    {
        UserId = userId;
        Action = action;
        IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress;
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? "unknown" : userAgent;
        Details = details;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Nullable: some events (e.g. a failed login for an unknown email) have no resolvable user.</summary>
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    public AuditAction Action { get; private set; }
    public string IpAddress { get; private set; }

    /// <summary>Raw User-Agent header. Browser/device parsing, if wanted, happens at the read/reporting side, not on write.</summary>
    public string UserAgent { get; private set; }

    public string? Details { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}

using SmartHorse.Domain.Entities;
using SmartHorse.Domain.Enums;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>Repository abstraction for the append-only <see cref="AuditLog"/> table (Sprint 2 §6).</summary>
public interface IAuditLogRepository
{
    void Add(AuditLog auditLog);

    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        Guid? userId,
        AuditAction? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

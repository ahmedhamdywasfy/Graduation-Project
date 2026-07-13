using MediatR;
using SmartHorse.Application.Users.DTOs;
using SmartHorse.Domain.Enums;

namespace SmartHorse.Application.Users.Queries.GetUserAuditLogs;

/// <summary>
/// Administrator-only audit trail search (Sprint 2 §6; v0.2 §2.3 GET /admin/audit-logs).
/// UserId is optional — omit to search across all users.
/// </summary>
public record GetUserAuditLogsQuery(
    Guid? UserId = null,
    AuditAction? Action = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedAuditLogListDto>;

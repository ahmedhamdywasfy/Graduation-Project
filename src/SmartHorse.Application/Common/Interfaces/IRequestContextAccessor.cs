namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Exposes request-scoped context (caller IP, User-Agent) to the Application layer
/// without a dependency on HttpContext — mirrors <see cref="ICurrentUserService"/>.
/// Used to populate <see cref="SmartHorse.Domain.Entities.AuditLog"/> entries and refresh
/// token metadata (Sprint 2 §4, §6).
/// </summary>
public interface IRequestContextAccessor
{
    string IpAddress { get; }

    string UserAgent { get; }
}

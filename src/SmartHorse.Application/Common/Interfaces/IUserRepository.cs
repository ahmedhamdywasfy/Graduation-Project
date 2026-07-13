using SmartHorse.Application.Common.Models;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for <see cref="User"/> (v0.1 Section 11 — Repository
/// Pattern). Implemented in the Infrastructure layer against EF Core; Application
/// layer handlers depend only on this interface, never on DbContext directly.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Includes UserRoles/Role — the common case for login/auth flows that need role claims.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Administrator user search (Sprint 2 §7 — pagination, sorting, filtering by name/email/role/status/created date).</summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        UserSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    void Add(User user);

    void Update(User user);
}

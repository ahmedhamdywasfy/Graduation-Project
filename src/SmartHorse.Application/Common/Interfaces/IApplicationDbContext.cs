using Microsoft.EntityFrameworkCore;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Narrow abstraction over the EF Core DbContext exposed to the Application layer.
/// Only what handlers legitimately need (SaveChangesAsync + read-only DbSets for
/// cross-aggregate queries) is exposed; repositories remain the primary write path.
/// This keeps EF Core itself an Infrastructure concern (v0.1 Section 10) while still
/// allowing efficient LINQ read queries from the Application layer for Sprint 1's
/// user-listing/dashboard-style queries.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermissionOverride> UserPermissionOverrides { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Common;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext — the only class in the solution allowed to know about EF
/// Core's DbSet/DbContext types outside of this Infrastructure project (v0.1
/// Section 10). Implements <see cref="IApplicationDbContext"/> so the Application
/// layer can depend on the abstraction only.
///
/// Sprint 1 mapped the Identity/User Management tables (v0.1 Section 13:
/// Users, Roles, UserRoles; v0.2 Section 2.2: Permissions, RolePermissions,
/// UserPermissionOverrides; v0.2 Section 8: RefreshTokens). Sprint 2 adds
/// AuditLogs (Sprint 2 §6) and extends User/RefreshToken with new columns —
/// no structural change to this class was needed for that, only new
/// EntityTypeConfigurations (auto-discovered below) and a migration.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stamps UpdatedAt on any modified BaseAuditableEntity automatically, so
    /// command handlers never need to set it manually (v0.1 Section 28 — no
    /// duplicated bookkeeping logic scattered across handlers).
    /// </summary>
    private void ApplyAuditInfo()
    {
        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}

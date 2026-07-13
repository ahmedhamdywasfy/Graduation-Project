using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartHorse.Application.Common.Interfaces;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Infrastructure.Persistence.Seed;

/// <summary>
/// Idempotent startup seeding for: Roles (v0.1 Section 4), a baseline Permissions
/// set + RolePermissions defaults (v0.2 Section 2.2), and one Administrator account
/// (v0.1 Section 21 checklist item "Seed Administrator"). Called once from
/// Program.cs on startup, guarded by "if not already present" checks so it is safe
/// to run on every deployment.
///
/// Administrator credentials are read from configuration (environment variables /
/// user secrets in development, Key Vault in production — v0.1 Section 21) and are
/// never hardcoded, per the project's secrets-management rule.
/// </summary>
public class DbSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Relational providers (SQL Server in production/development) apply pending
        // migrations; the EF Core InMemory provider used by
        // SmartHorse.API.IntegrationTests (Sprint 2 §14) does not support
        // migrations at all, so it falls back to EnsureCreated instead. This keeps
        // the same seeding path exercised by both real runs and integration tests.
        if (_context.Database.IsRelational())
        {
            await _context.Database.MigrateAsync();
        }
        else
        {
            await _context.Database.EnsureCreatedAsync();
        }

        var roles = await SeedRolesAsync();
        await SeedPermissionsAsync(roles);
        await SeedAdministratorAsync(roles);

        await _context.SaveChangesAsync();
    }

    private async Task<Dictionary<string, Role>> SeedRolesAsync()
    {
        var existing = await _context.Roles.ToDictionaryAsync(r => r.Name);

        foreach (var name in Role.Names.All)
        {
            if (!existing.ContainsKey(name))
            {
                var role = new Role(name);
                _context.Roles.Add(role);
                existing[name] = role;
                _logger.LogInformation("Seeded role {RoleName}.", name);
            }
        }

        await _context.SaveChangesAsync();
        return existing;
    }

    private async Task SeedPermissionsAsync(Dictionary<string, Role> roles)
    {
        // Sprint 1 seeds only Identity/User-Management-scoped permissions.
        // Module-specific permissions (horses.*, medical.*, marketplace.*, ...)
        // are added by the sprints that implement those modules (v0.2 Section 2.2).
        var permissionSeedList = new (string Key, string Description)[]
        {
            ("users.view", "View user accounts"),
            ("users.manage", "Create, deactivate, and edit user accounts"),
            ("users.unlock", "Manually unlock a locked-out user account"),
            ("roles.manage", "Assign and revoke roles"),
            ("permissions.manage", "Configure role and per-user permissions"),
            ("audit.view", "View the system audit log")
        };

        var existingPermissions = await _context.Permissions.ToDictionaryAsync(p => p.Key);

        foreach (var (key, description) in permissionSeedList)
        {
            if (!existingPermissions.ContainsKey(key))
            {
                var permission = new Permission(key, description);
                _context.Permissions.Add(permission);
                existingPermissions[key] = permission;
            }
        }

        await _context.SaveChangesAsync();

        // Administrator gets every seeded permission by default; other roles get none
        // at this stage (module sprints will add their own RolePermissions rows).
        if (roles.TryGetValue(Role.Names.Administrator, out var adminRole))
        {
            var existingRolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == adminRole.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            foreach (var permission in existingPermissions.Values)
            {
                if (!existingRolePermissions.Contains(permission.Id))
                {
                    _context.RolePermissions.Add(new RolePermission(adminRole.Id, permission.Id));
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedAdministratorAsync(Dictionary<string, Role> roles)
    {
        var adminEmail = _configuration["Seed:AdminEmail"];
        var adminPassword = _configuration["Seed:AdminPassword"];
        var adminFullName = _configuration["Seed:AdminFullName"] ?? "System Administrator";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogWarning(
                "Seed:AdminEmail / Seed:AdminPassword not configured — skipping Administrator seed. " +
                "Set these via environment variables or user secrets before first run.");
            return;
        }

        var normalizedEmail = adminEmail.Trim().ToLowerInvariant();
        var alreadyExists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);

        if (alreadyExists)
        {
            return;
        }

        if (!roles.TryGetValue(Role.Names.Administrator, out var adminRole))
        {
            _logger.LogError("Administrator role not found during seeding — roles must be seeded first.");
            return;
        }

        var passwordHash = _passwordHasher.Hash(adminPassword);
        var admin = new User(adminFullName, normalizedEmail, passwordHash);
        admin.AssignRole(adminRole);

        _context.Users.Add(admin);
        _logger.LogInformation("Seeded initial Administrator account ({Email}).", normalizedEmail);
    }
}

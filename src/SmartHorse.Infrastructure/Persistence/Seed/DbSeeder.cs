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
        await SeedHorseLookupDataAsync();

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
            ("audit.view", "View the system audit log"),
            ("horses.view", "View horse records"),
            ("horses.manage", "Create, update, delete, and restore horse records")
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

        // Compute every role's full target permission set in memory first, then
        // diff against what's already persisted exactly once — avoids adding the
        // same (RoleId, PermissionId) pair twice across the different grant rules
        // below before a single SaveChangesAsync flushes them all.
        var targetGrants = new Dictionary<int, HashSet<int>>();

        void AddGrant(int roleId, int permissionId)
        {
            if (!targetGrants.TryGetValue(roleId, out var set))
            {
                set = new HashSet<int>();
                targetGrants[roleId] = set;
            }

            set.Add(permissionId);
        }

        // Administrator gets every seeded permission by default.
        if (roles.TryGetValue(Role.Names.Administrator, out var adminRole))
        {
            foreach (var permission in existingPermissions.Values)
            {
                AddGrant(adminRole.Id, permission.Id);
            }
        }

        // Person 2 Sprint 1 §12 — only Administrator, Owner, and Veterinarian can
        // create/update/delete/restore horses; every other role is read-only.
        // (Actual enforcement this sprint uses the role-based "CanManageHorses"
        // policy in AuthenticationExtensions, same mechanism as "RequireAdministrator" —
        // these RolePermissions rows establish the data for a future fine-grained
        // permission-based authorization handler, per v0.2 §2.2.)
        if (existingPermissions.TryGetValue("horses.manage", out var horsesManagePermission))
        {
            foreach (var roleName in new[] { Role.Names.Owner, Role.Names.Veterinarian })
            {
                if (roles.TryGetValue(roleName, out var role))
                {
                    AddGrant(role.Id, horsesManagePermission.Id);
                }
            }
        }

        if (existingPermissions.TryGetValue("horses.view", out var horsesViewPermission))
        {
            foreach (var role in roles.Values)
            {
                AddGrant(role.Id, horsesViewPermission.Id);
            }
        }

        var allExistingRolePermissions = await _context.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();
        var existingSet = allExistingRolePermissions.Select(rp => (rp.RoleId, rp.PermissionId)).ToHashSet();

        foreach (var (roleId, permissionIds) in targetGrants)
        {
            foreach (var permissionId in permissionIds)
            {
                if (!existingSet.Contains((roleId, permissionId)))
                {
                    _context.RolePermissions.Add(new RolePermission(roleId, permissionId));
                    existingSet.Add((roleId, permissionId));
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

    /// <summary>
    /// Seeds the small reference tables Horse Core depends on (Person 2 Sprint 1
    /// §1). Idempotent — same "if not already present" pattern as SeedRolesAsync —
    /// safe to run on every deployment. Written as four explicit blocks (rather
    /// than a generic reflection-based helper) to match the rest of this class's style.
    /// </summary>
    private async Task SeedHorseLookupDataAsync()
    {
        var breedNames = new[] { "Arabian", "Thoroughbred", "Quarter Horse", "Andalusian", "Friesian", "Appaloosa", "Mustang" };
        var existingBreeds = (await _context.Breeds.Select(b => b.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in breedNames)
        {
            if (!existingBreeds.Contains(name))
            {
                _context.Breeds.Add(new Breed(name));
            }
        }

        var colorNames = new[] { "Bay", "Black", "Chestnut", "Grey", "Palomino", "Roan", "Buckskin", "White" };
        var existingColors = (await _context.Colors.Select(c => c.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in colorNames)
        {
            if (!existingColors.Contains(name))
            {
                _context.Colors.Add(new Color(name));
            }
        }

        var genderNames = new[] { "Stallion", "Mare", "Gelding", "Colt", "Filly" };
        var existingGenders = (await _context.Genders.Select(g => g.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in genderNames)
        {
            if (!existingGenders.Contains(name))
            {
                _context.Genders.Add(new Gender(name));
            }
        }

        var existingStatuses = (await _context.HorseStatuses.Select(s => s.Name).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in HorseStatus.Names.All)
        {
            if (!existingStatuses.Contains(name))
            {
                _context.HorseStatuses.Add(new HorseStatus(name));
            }
        }

        await _context.SaveChangesAsync();
    }
}

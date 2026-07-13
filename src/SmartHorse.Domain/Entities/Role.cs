using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// A system role (Owner, Veterinarian, Trainer, Worker, Buyer, Administrator), as
/// defined in v0.1 Section 4 (Target Users) and Section 13 (Database Planning).
/// Roles are a small, admin-managed reference table — seeded at startup (see
/// Infrastructure/Persistence/Seed/DbSeeder.cs) and rarely modified afterward.
/// </summary>
public class Role : BaseIntEntity
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<RolePermission> _rolePermissions = new();

    private Role()
    {
        // Required by EF Core.
        Name = string.Empty;
    }

    public Role(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public string Name { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    /// <summary>
    /// Well-known role names, matching v0.1 Section 4 (Target Users). Using constants
    /// instead of a hard enum keeps Roles a genuine editable database table (v0.2
    /// Section 2 Administration Module — Role Management) while still giving the
    /// codebase compile-time-safe references for policy/attribute definitions.
    /// </summary>
    public static class Names
    {
        public const string Owner = "Owner";
        public const string Veterinarian = "Veterinarian";
        public const string Trainer = "Trainer";
        public const string Worker = "Worker";
        public const string Buyer = "Buyer";
        public const string Administrator = "Administrator";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Owner, Veterinarian, Trainer, Worker, Buyer, Administrator
        };
    }
}

using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between <see cref="User"/> and
/// <see cref="Role"/> (v0.1 Section 13 — a user may hold more than one role, e.g.
/// Owner + Buyer). Modeled as an explicit entity (rather than a plain EF Core
/// skip-navigation) so it can later carry metadata such as AssignedAt if needed.
/// </summary>
public class UserRole
{
    private UserRole()
    {
        // Required by EF Core.
    }

    public UserRole(Guid userId, int roleId)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Overload that also wires the <see cref="Role"/> navigation property
    /// directly, so entities built purely in-memory (e.g. in unit tests that
    /// never go through EF Core's query materialization) are self-consistent
    /// without relying on EF's change-tracker relationship fixup. Used by
    /// <see cref="User.AssignRole"/> and <see cref="User.ReplaceRoles"/>.
    /// </summary>
    public UserRole(Guid userId, Role role)
    {
        UserId = userId;
        RoleId = role.Id;
        Role = role;
        AssignedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public int RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public DateTime AssignedAt { get; private set; }
}

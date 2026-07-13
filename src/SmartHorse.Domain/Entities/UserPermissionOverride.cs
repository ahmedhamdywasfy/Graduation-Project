namespace SmartHorse.Domain.Entities;

/// <summary>
/// A per-user exception to their role's default permission set (v0.2 Section 2.2)
/// — either granting a permission the role doesn't normally have, or revoking one
/// it does. Lets small stables handle one-off exceptions without creating a new
/// role for every combination.
/// </summary>
public class UserPermissionOverride
{
    private UserPermissionOverride()
    {
    }

    public UserPermissionOverride(Guid userId, int permissionId, bool isGranted)
    {
        UserId = userId;
        PermissionId = permissionId;
        IsGranted = isGranted;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    /// <summary>True = explicitly grants; False = explicitly revokes.</summary>
    public bool IsGranted { get; private set; }
}

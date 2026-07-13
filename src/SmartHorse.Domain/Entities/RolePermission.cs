namespace SmartHorse.Domain.Entities;

/// <summary>
/// Join entity granting a <see cref="Permission"/> to a <see cref="Role"/> by
/// default (v0.2 Section 2.2). The authorization pipeline (Application layer)
/// merges this default set with any <see cref="UserPermissionOverride"/> rows
/// for the specific user being authorized.
/// </summary>
public class RolePermission
{
    private RolePermission()
    {
    }

    public RolePermission(int roleId, int permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public int RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;
}

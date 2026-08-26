namespace SmartHorse.Domain.Common;

/// <summary>
/// Adds soft-delete and full actor-tracking audit fields on top of
/// <see cref="BaseAuditableEntity"/> (Person 2 Sprint 1 — Horse Core requires
/// CreatedBy/UpdatedBy plus soft delete; the shared Sprint 1/2 identity entities
/// do not need these, which is why this is a new derived base rather than an
/// extension of <see cref="BaseAuditableEntity"/> itself — no existing entity's
/// table changes as a result).
/// </summary>
public abstract class SoftDeletableAuditableEntity : BaseAuditableEntity
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public void MarkDeleted(Guid? deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}

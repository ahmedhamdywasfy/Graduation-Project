namespace SmartHorse.Domain.Common;

/// <summary>
/// Adds creation/modification audit fields on top of <see cref="BaseEntity"/>.
/// EF Core populates these automatically in ApplicationDbContext.SaveChangesAsync
/// (Infrastructure layer) rather than requiring callers to set them manually.
/// </summary>
public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

namespace SmartHorse.Domain.Common;

/// <summary>
/// Base class for entities that use a GUID surrogate key.
/// Most domain aggregates (User, RefreshToken, etc.) derive from this.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}

/// <summary>
/// Base class for entities that use an integer surrogate key.
/// Reserved for small, rarely-changing reference/lookup tables (Role, Permission),
/// consistent with the v0.1/v0.2 architecture documents.
/// </summary>
public abstract class BaseIntEntity
{
    public int Id { get; protected set; }
}

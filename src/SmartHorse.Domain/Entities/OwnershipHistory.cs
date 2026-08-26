using SmartHorse.Domain.Common;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// Ownership transfer audit trail for a <see cref="Horse"/> — Person 2 Sprint 1
/// Database Design §1, matching v0.1 §13's HorseOwnershipHistory. Append-only:
/// written once per ownership change and never updated, consistent with the
/// existing <see cref="AuditLog"/> pattern from Person 1 Sprint 2.
/// </summary>
public class OwnershipHistory : BaseEntity
{
    private OwnershipHistory()
    {
        // Required by EF Core.
    }

    public OwnershipHistory(Guid horseId, Guid? previousOwnerId, Guid newOwnerId, string? notes)
    {
        HorseId = horseId;
        PreviousOwnerId = previousOwnerId;
        NewOwnerId = newOwnerId;
        Notes = notes?.Trim();
        ChangedAtUtc = DateTime.UtcNow;
    }

    public Guid HorseId { get; private set; }
    public Horse Horse { get; private set; } = null!;

    /// <summary>Null for the very first ownership record (initial registration).</summary>
    public Guid? PreviousOwnerId { get; private set; }
    public User? PreviousOwner { get; private set; }

    public Guid NewOwnerId { get; private set; }
    public User NewOwner { get; private set; } = null!;

    public string? Notes { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
}

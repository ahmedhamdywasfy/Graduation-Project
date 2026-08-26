using SmartHorse.Domain.Common;
using SmartHorse.Domain.Exceptions;

namespace SmartHorse.Domain.Entities;

/// <summary>
/// Ownership transfer audit trail for a <see cref="Horse"/> — originally added in
/// Person 2 Sprint 1 (matching v0.1 §13's HorseOwnershipHistory), extended in
/// Sprint 2 with <see cref="SaleDate"/> and soft-delete support for the full
/// Ownership Module (§1). <see cref="BaseAuditableEntity.CreatedAt"/> (inherited
/// via <see cref="SoftDeletableAuditableEntity"/>) is the moment this record was
/// written; <see cref="PurchaseDate"/> is the ChangedAtUtc value already used
/// since Sprint 1 — kept under its original name/column to avoid an unnecessary
/// rename of completed Sprint 1 work, but is exactly what Sprint 2 §1 calls
/// "Purchase Date" and is exposed under that name in <c>OwnershipHistoryDto</c>.
/// </summary>
public class OwnershipHistory : SoftDeletableAuditableEntity
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

    /// <summary>The moment this ownership stint began — Sprint 2 §1 "Purchase Date".</summary>
    public DateTime ChangedAtUtc { get; private set; }

    /// <summary>
    /// The moment this ownership stint ended (set on the previously-current
    /// record when a new transfer happens). Null while this is still the
    /// active/current ownership record — Sprint 2 §1 "Sale Date".
    /// </summary>
    public DateTime? SaleDate { get; private set; }

    public bool IsActive => SaleDate is null && !IsDeleted;

    /// <summary>Called on the previously-current record when a new owner takes over.</summary>
    public void CloseOut(DateTime saleDateUtc)
    {
        SaleDate = saleDateUtc;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Administrator correction of a historical record's notes/dates — Sprint 2 §2 "Update Ownership".</summary>
    public void UpdateRecord(string? notes, DateTime purchaseDateUtc, DateTime? saleDateUtc, Guid updatedBy)
    {
        Notes = notes?.Trim();
        ChangedAtUtc = purchaseDateUtc;
        SaleDate = saleDateUtc;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(Guid? deletedBy)
    {
        if (IsDeleted)
        {
            throw new OwnershipRecordAlreadyDeletedException(Id);
        }

        MarkDeleted(deletedBy);
    }
}

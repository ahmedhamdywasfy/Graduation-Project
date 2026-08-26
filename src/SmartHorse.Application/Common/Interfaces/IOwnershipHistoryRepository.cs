using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for individual <see cref="OwnershipHistory"/> records
/// (Sprint 2 §1–§2 — Update/Delete a specific historical record by its own Id,
/// independent of loading the full <see cref="Horse"/> aggregate). Creating a
/// new record and closing out the previous one both still go through
/// <see cref="Horse.RecordOwnership"/> — this repository is only for direct,
/// single-record reads/corrections.
/// </summary>
public interface IOwnershipHistoryRepository
{
    Task<OwnershipHistory?> GetByIdAsync(Guid recordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OwnershipHistory>> GetByHorseIdAsync(Guid horseId, bool includeDeleted, CancellationToken cancellationToken = default);

    void Update(OwnershipHistory record);
}

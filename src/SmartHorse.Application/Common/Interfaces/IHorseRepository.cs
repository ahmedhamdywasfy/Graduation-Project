using SmartHorse.Application.Common.Models;
using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for <see cref="Horse"/> (Person 2 Sprint 1, mirroring
/// the existing <see cref="IUserRepository"/> pattern from Person 1). All read
/// methods on this interface implicitly respect the soft-delete query filter
/// configured in HorseConfiguration EXCEPT where a method name says otherwise
/// (IncludingDeleted / GetDeletedByIdAsync).
/// </summary>
public interface IHorseRepository
{
    /// <summary>Includes Breed/Color/Gender/Status (needed for HorseDto mapping) but not the larger Images/OwnershipHistory/CurrentOwner graph.</summary>
    Task<Horse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Includes Breed/Color/Gender/Status/CurrentOwner/Images/OwnershipHistory — the full detail-view graph.</summary>
    Task<Horse?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Bypasses the soft-delete filter — used by Restore, which must find an already-deleted horse.</summary>
    Task<Horse?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> MicrochipNumberExistsAsync(string microchipNumber, Guid? excludeHorseId = null, CancellationToken cancellationToken = default);

    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, Guid? excludeHorseId = null, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Horse> Items, int TotalCount)> GetPagedAsync(
        HorseSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    void Add(Horse horse);

    void Update(Horse horse);
}

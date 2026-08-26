using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>Repository abstraction for the small, seeded <see cref="Breed"/> lookup table.</summary>
public interface IBreedRepository
{
    Task<Breed?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Breed>> GetAllAsync(CancellationToken cancellationToken = default);
}

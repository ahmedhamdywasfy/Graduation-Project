using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>Repository abstraction for the small, seeded <see cref="HorseStatus"/> lookup table.</summary>
public interface IHorseStatusRepository
{
    Task<HorseStatus?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<HorseStatus?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HorseStatus>> GetAllAsync(CancellationToken cancellationToken = default);
}

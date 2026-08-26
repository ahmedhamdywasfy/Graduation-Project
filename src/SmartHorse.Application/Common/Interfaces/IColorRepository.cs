using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>Repository abstraction for the small, seeded <see cref="Color"/> lookup table.</summary>
public interface IColorRepository
{
    Task<Color?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Color>> GetAllAsync(CancellationToken cancellationToken = default);
}

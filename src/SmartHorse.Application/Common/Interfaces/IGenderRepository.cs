using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>Repository abstraction for the small, seeded <see cref="Gender"/> lookup table.</summary>
public interface IGenderRepository
{
    Task<Gender?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Gender>> GetAllAsync(CancellationToken cancellationToken = default);
}

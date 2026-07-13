using SmartHorse.Domain.Entities;

namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Repository abstraction for <see cref="RefreshToken"/>. Tokens are looked up by
/// their hash (never by raw value, which is never persisted) — see v0.2 Security
/// Review, Section 8.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(RefreshToken refreshToken);
}

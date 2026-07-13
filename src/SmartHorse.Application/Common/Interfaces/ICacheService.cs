namespace SmartHorse.Application.Common.Interfaces;

/// <summary>
/// Generic cache abstraction (Sprint 2 §12). The Sprint 2 Infrastructure
/// implementation wraps <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>;
/// the interface itself is deliberately shaped like <c>IDistributedCache</c>'s usage
/// pattern (string key, TTL, typed get/set) so swapping in a Redis-backed
/// implementation later (v0.2 §11 scalability path) requires no Application-layer
/// changes — only a new Infrastructure registration.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using SmartHorse.Application.Common.Interfaces;

namespace SmartHorse.Infrastructure.Services;

/// <summary>
/// <see cref="IMemoryCache"/>-backed implementation of <see cref="ICacheService"/>
/// (Sprint 2 §12). Tracks its own key set (<see cref="IMemoryCache"/> has no
/// native "list/remove by prefix" API) so <see cref="RemoveByPrefixAsync"/> can
/// invalidate a whole family of keys, e.g. all cached pages of a user search
/// after a user is created/updated.
///
/// Swapping to Redis later (v0.2 §11 scalability path) means creating a
/// <c>RedisCacheService</c> implementing this same interface (Redis's key-pattern
/// scan natively supports prefix removal) and changing one DI registration —
/// no Application-layer or handler code changes.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) where T : class
    {
        _cache.Set(key, value, expiration);
        _keys.TryAdd(key, 0);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}

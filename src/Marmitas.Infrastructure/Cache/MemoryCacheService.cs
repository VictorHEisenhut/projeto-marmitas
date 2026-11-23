using System.Collections.Concurrent;
using Marmitas.Application.Common.Interfaces;

namespace Marmitas.Infrastructure.Cache;

public sealed class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (object? Value, DateTime ExpiresAt)> _entries = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow && entry.Value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            _entries.TryRemove(key, out _);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        _entries[key] = (value, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}

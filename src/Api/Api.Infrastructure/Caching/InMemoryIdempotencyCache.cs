using Api.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Infrastructure.Caching;

public class InMemoryIdempotencyCache(IMemoryCache cache) : IIdempotencyCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    public Task<IdempotencyEntry?> GetAsync(string key, CancellationToken ct = default)
    {
        cache.TryGetValue(key, out IdempotencyEntry? entry);
        return Task.FromResult(entry);
    }

    public Task SetAsync(string key, IdempotencyEntry entry, CancellationToken ct = default)
    {
        cache.Set(key, entry, Ttl);
        return Task.CompletedTask;
    }
}

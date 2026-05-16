using Api.Application.Common.Interfaces;
using ControlPlane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ControlPlane.Infrastructure.TenantSettings;

public sealed class DbTenantSettings(
    ControlPlaneDbContext db,
    IMemoryCache cache,
    ITenantContext tenantContext) : ITenantSettings
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private async Task<Dictionary<string, string>> LoadAsync()
    {
        var tenantId = tenantContext.TenantId;
        var cacheKey = $"tenant_settings:{tenantId}";

        if (cache.TryGetValue<Dictionary<string, string>>(cacheKey, out var cached) && cached is not null)
            return cached;

        var settings = await db.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Settings)
            .FirstOrDefaultAsync();

        var dict = settings?.ToDictionary(s => s.Key, s => s.Value)
                   ?? new Dictionary<string, string>();

        cache.Set(cacheKey, dict, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheTtl
        });

        return dict;
    }

    public T Get<T>(string key, T defaultValue)
    {
        var dict = LoadAsync().GetAwaiter().GetResult();
        if (!dict.TryGetValue(key, out var raw)) return defaultValue;
        try { return (T)Convert.ChangeType(raw, typeof(T)); }
        catch { return defaultValue; }
    }

    public bool GetBool(string key, bool defaultValue = false) => Get(key, defaultValue);
    public int GetInt(string key, int defaultValue = 0) => Get(key, defaultValue);
    public decimal GetDecimal(string key, decimal defaultValue = 0m) => Get(key, defaultValue);
    public string GetString(string key, string defaultValue = "") => Get(key, defaultValue);
}

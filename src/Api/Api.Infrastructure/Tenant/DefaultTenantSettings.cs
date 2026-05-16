using Api.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Api.Infrastructure.Tenant;

/// <summary>
/// In-memory tenant settings backed by appsettings.json (section "TenantSettings").
/// In E3.5 this is replaced by a DB-backed implementation with per-tenant rows and cache.
/// </summary>
public sealed class DefaultTenantSettings(IConfiguration configuration) : ITenantSettings
{
    private readonly IConfigurationSection _section =
        configuration.GetSection("TenantSettings");

    public T Get<T>(string key, T defaultValue)
    {
        var value = _section[key];
        if (value is null) return defaultValue;

        try { return (T)Convert.ChangeType(value, typeof(T)); }
        catch { return defaultValue; }
    }

    public bool GetBool(string key, bool defaultValue = false) =>
        Get(key, defaultValue);

    public int GetInt(string key, int defaultValue = 0) =>
        Get(key, defaultValue);

    public decimal GetDecimal(string key, decimal defaultValue = 0m) =>
        Get(key, defaultValue);

    public string GetString(string key, string defaultValue = "") =>
        Get(key, defaultValue);
}

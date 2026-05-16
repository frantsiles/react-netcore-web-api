using ControlPlane.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ControlPlane.Infrastructure.TenantSettings;

public sealed class MemoryTenantSettingsCache(IMemoryCache cache) : ITenantSettingsCache
{
    public void Invalidate(Guid tenantId) =>
        cache.Remove($"tenant_settings:{tenantId}");
}

using Api.Application.Common.Interfaces;

namespace Api.Infrastructure.Persistence;

/// <summary>
/// ITenantContext stub used only by IDesignTimeDbContextFactory implementations.
/// Never registered in the DI container at runtime.
/// </summary>
public sealed class DesignTimeTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string CountryCode => "US";
}

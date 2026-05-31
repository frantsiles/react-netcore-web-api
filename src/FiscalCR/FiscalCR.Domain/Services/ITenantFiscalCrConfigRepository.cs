using FiscalCR.Domain.TenantConfig;

namespace FiscalCR.Domain.Services;

public interface ITenantFiscalCrConfigRepository
{
    Task<TenantFiscalCrConfig?> FindByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(TenantFiscalCrConfig config, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

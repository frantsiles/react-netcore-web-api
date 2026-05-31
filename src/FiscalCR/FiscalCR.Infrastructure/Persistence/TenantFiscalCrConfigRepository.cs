using FiscalCR.Domain.Services;
using FiscalCR.Domain.TenantConfig;
using Microsoft.EntityFrameworkCore;

namespace FiscalCR.Infrastructure.Persistence;

public class TenantFiscalCrConfigRepository(FiscalCrDbContext db) : ITenantFiscalCrConfigRepository
{
    public Task<TenantFiscalCrConfig?> FindByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => db.TenantConfigs.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

    public async Task AddAsync(TenantFiscalCrConfig config, CancellationToken ct = default)
        => await db.TenantConfigs.AddAsync(config, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}

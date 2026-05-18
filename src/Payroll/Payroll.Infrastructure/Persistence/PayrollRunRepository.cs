using Microsoft.EntityFrameworkCore;
using Payroll.Domain.PayrollRuns;
using Payroll.Domain.Services;

namespace Payroll.Infrastructure.Persistence;

public class PayrollRunRepository(PayrollDbContext db) : IPayrollRunRepository
{
    public async Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.PayrollRuns.Include(r => r.Entries).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<List<PayrollRun>> ListByTenantAsync(
        Guid tenantId, int skip, int take, CancellationToken ct = default)
        => await db.PayrollRuns
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.PeriodStart)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(PayrollRun run, CancellationToken ct = default)
        => await db.PayrollRuns.AddAsync(run, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}

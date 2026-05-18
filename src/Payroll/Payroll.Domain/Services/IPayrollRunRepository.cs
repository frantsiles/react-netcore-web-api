using Payroll.Domain.PayrollRuns;

namespace Payroll.Domain.Services;

public interface IPayrollRunRepository
{
    Task<PayrollRun?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<PayrollRun>> ListByTenantAsync(Guid tenantId, int skip, int take, CancellationToken ct);
    Task AddAsync(PayrollRun run, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

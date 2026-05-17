using HR.Domain.Contracts;
using HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Persistence.Repositories;

public class ContractRepository(HrDbContext db) : IContractRepository
{
    public async Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Contracts.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<Contract>> ListByEmployeeAsync(Guid employeeId, CancellationToken ct = default) =>
        await db.Contracts.Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.StartDate).ToListAsync(ct);

    public async Task<Contract?> GetActiveContractAsync(Guid employeeId, CancellationToken ct = default) =>
        await db.Contracts.FirstOrDefaultAsync(
            c => c.EmployeeId == employeeId && c.Status == ContractStatus.Active, ct);

    public async Task AddAsync(Contract contract, CancellationToken ct = default)
    {
        await db.Contracts.AddAsync(contract, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Contract contract, CancellationToken ct = default)
    {
        db.Contracts.Update(contract);
        await db.SaveChangesAsync(ct);
    }
}

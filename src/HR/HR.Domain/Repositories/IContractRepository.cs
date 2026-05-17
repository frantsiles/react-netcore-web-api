using HR.Domain.Contracts;

namespace HR.Domain.Repositories;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Contract>> ListByEmployeeAsync(Guid employeeId, CancellationToken ct = default);
    Task<Contract?> GetActiveContractAsync(Guid employeeId, CancellationToken ct = default);
    Task AddAsync(Contract contract, CancellationToken ct = default);
    Task UpdateAsync(Contract contract, CancellationToken ct = default);
}

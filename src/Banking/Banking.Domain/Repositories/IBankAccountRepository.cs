using Banking.Domain.BankAccounts;

namespace Banking.Domain.Repositories;

public interface IBankAccountRepository
{
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByAccountNumberAsync(Guid tenantId, string accountNumber, CancellationToken ct = default);
    Task<IReadOnlyList<BankAccount>> ListAsync(Guid tenantId, BankAccountStatus? status, CancellationToken ct = default);
    Task AddAsync(BankAccount account, CancellationToken ct = default);
    Task UpdateAsync(BankAccount account, CancellationToken ct = default);
}

using Accounting.Domain.Accounts;

namespace Accounting.Domain.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByNumberAsync(Guid tenantId, string accountNumber, CancellationToken ct = default);
    Task<bool> ExistsByNumberAsync(Guid tenantId, string accountNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> ListAsync(Guid tenantId, AccountType? type, bool? isActive, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task UpdateAsync(Account account, CancellationToken ct = default);
}

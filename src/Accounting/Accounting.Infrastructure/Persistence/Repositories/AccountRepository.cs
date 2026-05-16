using Accounting.Domain.Accounts;
using Accounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence.Repositories;

public class AccountRepository(AccountingDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Account?> GetByNumberAsync(Guid tenantId, string accountNumber, CancellationToken ct = default) =>
        db.Accounts.FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AccountNumber == accountNumber, ct);

    public Task<bool> ExistsByNumberAsync(Guid tenantId, string accountNumber, CancellationToken ct = default) =>
        db.Accounts.AnyAsync(a => a.TenantId == tenantId && a.AccountNumber == accountNumber, ct);

    public async Task<IReadOnlyList<Account>> ListAsync(
        Guid tenantId, AccountType? type, bool? isActive, CancellationToken ct = default)
    {
        var query = db.Accounts.Where(a => a.TenantId == tenantId).AsQueryable();
        if (type.HasValue) query = query.Where(a => a.Type == type.Value);
        if (isActive.HasValue) query = query.Where(a => a.IsActive == isActive.Value);
        return await query.OrderBy(a => a.AccountNumber).ToListAsync(ct);
    }

    public async Task AddAsync(Account account, CancellationToken ct = default)
    {
        await db.Accounts.AddAsync(account, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Account account, CancellationToken ct = default)
    {
        db.Accounts.Update(account);
        await db.SaveChangesAsync(ct);
    }
}

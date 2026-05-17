using Banking.Domain.BankAccounts;
using Banking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence.Repositories;

public class BankAccountRepository(BankingDbContext db) : IBankAccountRepository
{
    public Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.BankAccounts.Include("_transactions").FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExistsByAccountNumberAsync(Guid tenantId, string accountNumber, CancellationToken ct = default) =>
        db.BankAccounts.AnyAsync(a => a.TenantId == tenantId && a.AccountNumber == accountNumber, ct);

    public async Task<IReadOnlyList<BankAccount>> ListAsync(
        Guid tenantId, BankAccountStatus? status, CancellationToken ct = default)
    {
        var query = db.BankAccounts.Include("_transactions")
            .Where(a => a.TenantId == tenantId).AsQueryable();
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        return await query.OrderBy(a => a.BankName).ToListAsync(ct);
    }

    public async Task AddAsync(BankAccount account, CancellationToken ct = default)
    {
        await db.BankAccounts.AddAsync(account, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BankAccount account, CancellationToken ct = default)
    {
        db.BankAccounts.Update(account);
        await db.SaveChangesAsync(ct);
    }
}

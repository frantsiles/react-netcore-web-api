using Microsoft.EntityFrameworkCore;
using Parties.Domain.Parties;
using Parties.Domain.Repositories;

namespace Parties.Infrastructure.Persistence.Repositories;

public class PartyRepository(PartiesDbContext db) : IPartyRepository
{
    public async Task<Party?> GetByIdAsync(Guid partyId, CancellationToken ct = default)
        => await db.Parties
            .Include("_roles")
            .Include("_addresses")
            .Include("_contactPoints")
            .FirstOrDefaultAsync(p => p.Id == partyId, ct);

    public async Task<bool> ExistsByTaxIdAsync(string taxId, string countryCode, CancellationToken ct = default)
        => await db.Parties
            .AnyAsync(p => p.TaxId != null
                && p.TaxId.Value == taxId
                && p.TaxId.CountryCode == countryCode, ct);

    public async Task<IReadOnlyList<Party>> SearchAsync(
        string? legalName, PartyRoleType? roleType, bool? isActive,
        int skip, int take, CancellationToken ct = default)
    {
        var query = db.Parties.AsQueryable();

        if (!string.IsNullOrWhiteSpace(legalName))
            query = query.Where(p => p.LegalName.Contains(legalName));

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        return await query.Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task AddAsync(Party party, CancellationToken ct = default)
    {
        await db.Parties.AddAsync(party, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Party party, CancellationToken ct = default)
    {
        db.Parties.Update(party);
        await db.SaveChangesAsync(ct);
    }
}

using Parties.Domain.Parties;

namespace Parties.Domain.Repositories;

public interface IPartyRepository
{
    Task<Party?> GetByIdAsync(Guid partyId, CancellationToken ct = default);
    Task<bool> ExistsByTaxIdAsync(string taxId, string countryCode, CancellationToken ct = default);
    Task<IReadOnlyList<Party>> SearchAsync(string? legalName, PartyRoleType? roleType,
        bool? isActive, int skip, int take, CancellationToken ct = default);
    Task AddAsync(Party party, CancellationToken ct = default);
    Task UpdateAsync(Party party, CancellationToken ct = default);
}

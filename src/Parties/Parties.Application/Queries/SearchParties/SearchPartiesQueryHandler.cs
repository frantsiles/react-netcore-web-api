using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Application.Common.Mappings;
using Parties.Domain.Repositories;

namespace Parties.Application.Queries.SearchParties;

public class SearchPartiesQueryHandler(IPartyRepository partyRepository)
    : IRequestHandler<SearchPartiesQuery, IReadOnlyList<PartyDto>>
{
    public async Task<IReadOnlyList<PartyDto>> Handle(
        SearchPartiesQuery request, CancellationToken cancellationToken)
    {
        var parties = await partyRepository.SearchAsync(
            request.LegalName,
            request.RoleType,
            request.IsActive,
            request.Skip,
            request.Take,
            cancellationToken);

        return parties.Select(p => p.ToDto()).ToList();
    }
}

using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.Repositories;
using Parties.Application.Common.Mappings;

namespace Parties.Application.Queries.GetPartyById;

public class GetPartyByIdQueryHandler(IPartyRepository partyRepository)
    : IRequestHandler<GetPartyByIdQuery, PartyDto>
{
    public async Task<PartyDto> Handle(GetPartyByIdQuery request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        return party.ToDto();
    }
}

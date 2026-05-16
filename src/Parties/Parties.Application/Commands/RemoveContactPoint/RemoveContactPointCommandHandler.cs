using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Application.Common.Mappings;
using Parties.Domain.Repositories;

namespace Parties.Application.Commands.RemoveContactPoint;

public class RemoveContactPointCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<RemoveContactPointCommand, PartyDto>
{
    public async Task<PartyDto> Handle(RemoveContactPointCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        party.RemoveContactPoint(request.ContactPointIndex);
        await partyRepository.UpdateAsync(party, cancellationToken);

        return party.ToDto();
    }
}

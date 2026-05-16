using Api.Domain.Common;
using MediatR;
using Parties.Domain.Repositories;

namespace Parties.Application.Commands.DeactivateParty;

public class DeactivatePartyCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<DeactivatePartyCommand>
{
    public async Task Handle(DeactivatePartyCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        party.Deactivate();
        await partyRepository.UpdateAsync(party, cancellationToken);
    }
}

using Api.Domain.Common;
using MediatR;
using Parties.Domain.Repositories;

namespace Parties.Application.Commands.ReactivateParty;

public class ReactivatePartyCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<ReactivatePartyCommand>
{
    public async Task Handle(ReactivatePartyCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        party.Reactivate();
        await partyRepository.UpdateAsync(party, cancellationToken);
    }
}

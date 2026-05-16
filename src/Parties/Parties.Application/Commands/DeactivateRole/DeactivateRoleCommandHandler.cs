using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Application.Common.Mappings;
using Parties.Domain.Repositories;

namespace Parties.Application.Commands.DeactivateRole;

public class DeactivateRoleCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<DeactivateRoleCommand, PartyDto>
{
    public async Task<PartyDto> Handle(DeactivateRoleCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        party.DeactivateRole(request.RoleType);
        await partyRepository.UpdateAsync(party, cancellationToken);

        return party.ToDto();
    }
}

using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Application.Common.Mappings;
using Parties.Domain.Parties;
using Parties.Domain.Repositories;

namespace Parties.Application.Commands.ActivateRole;

public class ActivateRoleCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<ActivateRoleCommand, PartyDto>
{
    public async Task<PartyDto> Handle(ActivateRoleCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        party.ActivateRole(request.RoleType);
        await partyRepository.UpdateAsync(party, cancellationToken);

        return party.ToDto();
    }
}

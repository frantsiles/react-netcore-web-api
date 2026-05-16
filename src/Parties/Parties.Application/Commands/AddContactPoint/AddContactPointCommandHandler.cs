using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Application.Common.Mappings;
using Parties.Domain.Repositories;
using Parties.Domain.ValueObjects;

namespace Parties.Application.Commands.AddContactPoint;

public class AddContactPointCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<AddContactPointCommand, PartyDto>
{
    public async Task<PartyDto> Handle(AddContactPointCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        var contactPoint = ContactPoint.Create(request.Type, request.Value, request.IsPrimary);
        party.AddContactPoint(contactPoint);
        await partyRepository.UpdateAsync(party, cancellationToken);

        return party.ToDto();
    }
}

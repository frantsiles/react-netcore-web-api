using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Application.Common.Mappings;
using Parties.Domain.Repositories;

namespace Parties.Application.Commands.RemoveAddress;

public class RemoveAddressCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<RemoveAddressCommand, PartyDto>
{
    public async Task<PartyDto> Handle(RemoveAddressCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        party.RemoveAddress(request.AddressIndex);
        await partyRepository.UpdateAsync(party, cancellationToken);

        return party.ToDto();
    }
}

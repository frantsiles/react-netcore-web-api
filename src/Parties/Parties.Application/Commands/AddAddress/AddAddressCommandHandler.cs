using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Application.Common.Mappings;
using Parties.Domain.Repositories;
using Parties.Domain.ValueObjects;

namespace Parties.Application.Commands.AddAddress;

public class AddAddressCommandHandler(IPartyRepository partyRepository)
    : IRequestHandler<AddAddressCommand, PartyDto>
{
    public async Task<PartyDto> Handle(AddAddressCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        var address = Address.Create(
            request.Line1, request.Line2, request.City,
            request.StateOrRegion, request.PostalCode, request.CountryCode);

        party.AddAddress(address);
        await partyRepository.UpdateAsync(party, cancellationToken);

        return party.ToDto();
    }
}

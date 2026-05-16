using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.Repositories;
using Parties.Domain.ValueObjects;
using Parties.Application.Common.Mappings;

namespace Parties.Application.Commands.UpdatePartyProfile;

public class UpdatePartyProfileCommandHandler(
    IPartyRepository partyRepository,
    IEventPublisher eventPublisher) : IRequestHandler<UpdatePartyProfileCommand, PartyDto>
{
    public async Task<PartyDto> Handle(UpdatePartyProfileCommand request, CancellationToken cancellationToken)
    {
        var party = await partyRepository.GetByIdAsync(request.PartyId, cancellationToken)
            ?? throw new DomainException($"Party '{request.PartyId}' not found.");

        TaxId? taxId = null;
        if (!string.IsNullOrWhiteSpace(request.TaxId))
            taxId = TaxId.Create(request.TaxId, party.CountryCode);

        party.UpdateProfile(request.LegalName, request.TradeName, taxId);
        await partyRepository.UpdateAsync(party, cancellationToken);

        foreach (var domainEvent in party.DomainEvents)
            await eventPublisher.PublishAsync(domainEvent, cancellationToken);

        party.ClearDomainEvents();

        return party.ToDto();
    }
}

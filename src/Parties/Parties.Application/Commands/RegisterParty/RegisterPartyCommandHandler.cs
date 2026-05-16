using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.Parties;
using Parties.Domain.Repositories;
using Parties.Domain.ValueObjects;
using Parties.Application.Common.Mappings;

namespace Parties.Application.Commands.RegisterParty;

public class RegisterPartyCommandHandler(
    IPartyRepository partyRepository,
    IEventPublisher eventPublisher) : IRequestHandler<RegisterPartyCommand, PartyDto>
{
    public async Task<PartyDto> Handle(RegisterPartyCommand request, CancellationToken cancellationToken)
    {
        TaxId? taxId = null;
        if (!string.IsNullOrWhiteSpace(request.TaxId))
        {
            taxId = TaxId.Create(request.TaxId, request.CountryCode);

            if (await partyRepository.ExistsByTaxIdAsync(taxId.Value, taxId.CountryCode, cancellationToken))
                throw new DomainException($"A party with tax ID '{request.TaxId}' already exists.");
        }

        Money? creditLimit = null;
        if (request.CreditLimit.HasValue && !string.IsNullOrWhiteSpace(request.CreditLimitCurrency))
            creditLimit = Money.Of(request.CreditLimit.Value, request.CreditLimitCurrency);

        var firstRole = PartyRole.Create(
            request.FirstRoleType,
            creditLimit,
            request.PaymentTermsDays,
            request.EmployeeNumber);

        var party = Party.Create(
            request.LegalName,
            request.PartyType,
            request.CountryCode,
            firstRole,
            request.TradeName,
            taxId);

        await partyRepository.AddAsync(party, cancellationToken);

        foreach (var domainEvent in party.DomainEvents)
            await eventPublisher.PublishAsync(domainEvent, cancellationToken);

        party.ClearDomainEvents();

        return party.ToDto();
    }
}

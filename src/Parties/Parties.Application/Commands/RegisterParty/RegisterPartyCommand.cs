using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.Parties;

namespace Parties.Application.Commands.RegisterParty;

public record RegisterPartyCommand(
    string LegalName,
    string? TradeName,
    PartyType PartyType,
    string CountryCode,
    PartyRoleType FirstRoleType,
    string? TaxId,
    decimal? CreditLimit,
    string? CreditLimitCurrency,
    int? PaymentTermsDays,
    string? EmployeeNumber) : IRequest<PartyDto>;

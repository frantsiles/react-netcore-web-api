using MediatR;
using Parties.Application.Common.Dtos;

namespace Parties.Application.Commands.UpdatePartyProfile;

public record UpdatePartyProfileCommand(
    Guid PartyId,
    string LegalName,
    string? TradeName,
    string? TaxId) : IRequest<PartyDto>;

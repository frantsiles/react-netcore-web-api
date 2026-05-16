using MediatR;
using Parties.Application.Common.Dtos;

namespace Parties.Application.Commands.AddAddress;

public record AddAddressCommand(
    Guid PartyId,
    string Line1,
    string? Line2,
    string City,
    string StateOrRegion,
    string PostalCode,
    string CountryCode) : IRequest<PartyDto>;

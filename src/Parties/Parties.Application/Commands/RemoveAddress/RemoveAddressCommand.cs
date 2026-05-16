using MediatR;
using Parties.Application.Common.Dtos;

namespace Parties.Application.Commands.RemoveAddress;

public record RemoveAddressCommand(Guid PartyId, int AddressIndex) : IRequest<PartyDto>;

using MediatR;

namespace Parties.Application.Commands.DeactivateParty;

public record DeactivatePartyCommand(Guid PartyId) : IRequest;

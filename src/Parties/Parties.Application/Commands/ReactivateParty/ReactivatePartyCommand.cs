using MediatR;

namespace Parties.Application.Commands.ReactivateParty;

public record ReactivatePartyCommand(Guid PartyId) : IRequest;

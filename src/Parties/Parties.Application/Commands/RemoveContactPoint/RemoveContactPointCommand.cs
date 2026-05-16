using MediatR;
using Parties.Application.Common.Dtos;

namespace Parties.Application.Commands.RemoveContactPoint;

public record RemoveContactPointCommand(Guid PartyId, int ContactPointIndex) : IRequest<PartyDto>;

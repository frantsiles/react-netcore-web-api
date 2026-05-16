using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.ValueObjects;

namespace Parties.Application.Commands.AddContactPoint;

public record AddContactPointCommand(
    Guid PartyId,
    ContactPointType Type,
    string Value,
    bool IsPrimary = false) : IRequest<PartyDto>;

using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.Parties;

namespace Parties.Application.Commands.DeactivateRole;

public record DeactivateRoleCommand(Guid PartyId, PartyRoleType RoleType) : IRequest<PartyDto>;

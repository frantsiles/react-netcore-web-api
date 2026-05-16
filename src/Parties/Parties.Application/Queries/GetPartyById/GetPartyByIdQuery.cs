using MediatR;
using Parties.Application.Common.Dtos;

namespace Parties.Application.Queries.GetPartyById;

public record GetPartyByIdQuery(Guid PartyId) : IRequest<PartyDto>;

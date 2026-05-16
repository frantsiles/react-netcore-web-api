using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.Parties;

namespace Parties.Application.Queries.SearchParties;

public record SearchPartiesQuery(
    string? LegalName,
    PartyRoleType? RoleType,
    bool? IsActive,
    int Skip = 0,
    int Take = 20) : IRequest<IReadOnlyList<PartyDto>>;

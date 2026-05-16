using MediatR;
using Parties.Application.Common.Dtos;
using Parties.Domain.Parties;

namespace Parties.Application.Commands.ActivateRole;

public record ActivateRoleCommand(
    Guid PartyId,
    PartyRoleType RoleType,
    decimal? CreditLimit,
    string? CreditLimitCurrency,
    int? PaymentTermsDays,
    string? EmployeeNumber) : IRequest<PartyDto>;

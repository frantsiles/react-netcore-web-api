using FluentValidation;

namespace Parties.Application.Commands.DeactivateRole;

public class DeactivateRoleCommandValidator : AbstractValidator<DeactivateRoleCommand>
{
    public DeactivateRoleCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.RoleType).IsInEnum();
    }
}

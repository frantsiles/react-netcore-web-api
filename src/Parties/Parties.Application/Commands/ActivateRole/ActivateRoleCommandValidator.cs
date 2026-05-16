using FluentValidation;
using Parties.Domain.Parties;

namespace Parties.Application.Commands.ActivateRole;

public class ActivateRoleCommandValidator : AbstractValidator<ActivateRoleCommand>
{
    public ActivateRoleCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.RoleType).IsInEnum();
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).When(x => x.CreditLimit.HasValue);
        RuleFor(x => x.CreditLimitCurrency).Length(3).When(x => x.CreditLimit.HasValue)
            .WithMessage("Currency code required when credit limit is specified.");
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0).When(x => x.PaymentTermsDays.HasValue);
        RuleFor(x => x.RoleType)
            .Must(r => r == PartyRoleType.Customer || r == PartyRoleType.Supplier)
            .When(x => x.CreditLimit.HasValue)
            .WithMessage("Credit limit is only applicable to Customer or Supplier roles.");
    }
}

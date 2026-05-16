using FluentValidation;
using Parties.Domain.Parties;

namespace Parties.Application.Commands.RegisterParty;

public class RegisterPartyCommandValidator : AbstractValidator<RegisterPartyCommand>
{
    public RegisterPartyCommandValidator()
    {
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TradeName).MaximumLength(300).When(x => x.TradeName is not null);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
        RuleFor(x => x.PartyType).IsInEnum();
        RuleFor(x => x.FirstRoleType).IsInEnum();

        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0).When(x => x.CreditLimit.HasValue);
        RuleFor(x => x.CreditLimitCurrency).Length(3).When(x => x.CreditLimit.HasValue)
            .WithMessage("Currency code required when credit limit is specified.");
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0).When(x => x.PaymentTermsDays.HasValue);

        RuleFor(x => x.FirstRoleType)
            .Must(r => r == PartyRoleType.Customer || r == PartyRoleType.Supplier)
            .When(x => x.CreditLimit.HasValue)
            .WithMessage("Credit limit is only applicable to Customer or Supplier roles.");
    }
}

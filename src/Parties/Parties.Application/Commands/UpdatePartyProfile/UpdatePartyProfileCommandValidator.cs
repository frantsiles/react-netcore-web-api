using FluentValidation;

namespace Parties.Application.Commands.UpdatePartyProfile;

public class UpdatePartyProfileCommandValidator : AbstractValidator<UpdatePartyProfileCommand>
{
    public UpdatePartyProfileCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.TradeName).MaximumLength(300).When(x => x.TradeName is not null);
    }
}

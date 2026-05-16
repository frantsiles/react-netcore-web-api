using FluentValidation;

namespace Parties.Application.Commands.RemoveAddress;

public class RemoveAddressCommandValidator : AbstractValidator<RemoveAddressCommand>
{
    public RemoveAddressCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.AddressIndex).GreaterThanOrEqualTo(0);
    }
}

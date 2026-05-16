using FluentValidation;

namespace Parties.Application.Commands.AddContactPoint;

public class AddContactPointCommandValidator : AbstractValidator<AddContactPointCommand>
{
    public AddContactPointCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value).NotEmpty().MaximumLength(300);
    }
}

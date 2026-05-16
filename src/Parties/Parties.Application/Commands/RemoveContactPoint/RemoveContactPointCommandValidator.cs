using FluentValidation;

namespace Parties.Application.Commands.RemoveContactPoint;

public class RemoveContactPointCommandValidator : AbstractValidator<RemoveContactPointCommand>
{
    public RemoveContactPointCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.ContactPointIndex).GreaterThanOrEqualTo(0);
    }
}

using FluentValidation;

namespace Catalog.Application.Commands.UpdatePriceList;

public class UpdatePriceListCommandValidator : AbstractValidator<UpdatePriceListCommand>
{
    public UpdatePriceListCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ValidTo).Must((cmd, validTo) => !validTo.HasValue || validTo.Value > cmd.ValidFrom)
            .When(x => x.ValidTo.HasValue)
            .WithMessage("ValidTo must be after ValidFrom.");
    }
}

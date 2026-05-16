using FluentValidation;

namespace Catalog.Application.Commands.CreatePriceList;

public class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.ValidTo).Must((cmd, validTo) => !validTo.HasValue || validTo.Value > cmd.ValidFrom)
            .When(x => x.ValidTo.HasValue)
            .WithMessage("ValidTo must be after ValidFrom.");
    }
}

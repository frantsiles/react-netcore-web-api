using FluentValidation;

namespace Catalog.Application.Commands.AddPriceEntry;

public class AddPriceEntryCommandValidator : AbstractValidator<AddPriceEntryCommand>
{
    public AddPriceEntryCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinQuantity).GreaterThan(0);
    }
}

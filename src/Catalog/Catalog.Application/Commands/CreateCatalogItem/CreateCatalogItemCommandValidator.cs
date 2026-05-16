using Catalog.Domain.Catalog;
using FluentValidation;

namespace Catalog.Application.Commands.CreateCatalogItem;

public class CreateCatalogItemCommandValidator : AbstractValidator<CreateCatalogItemCommand>
{
    public CreateCatalogItemCommandValidator()
    {
        RuleFor(x => x.SKU).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.ItemType).IsInEnum();
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TaxCategory).NotEmpty().Must(t => new[] { "STANDARD", "EXEMPT", "REDUCED" }.Contains(t))
            .WithMessage("TaxCategory must be STANDARD, EXEMPT or REDUCED.");
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0).When(x => x.ReorderPoint.HasValue);
        RuleFor(x => x.TrackInventory).Must((cmd, track) => !(track && cmd.ItemType == ItemType.Service))
            .WithMessage("Services cannot track inventory.");
    }
}

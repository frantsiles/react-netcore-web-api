using FluentValidation;

namespace Catalog.Application.Commands.UpdateCatalogItem;

public class UpdateCatalogItemCommandValidator : AbstractValidator<UpdateCatalogItemCommand>
{
    public UpdateCatalogItemCommandValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TaxCategory).NotEmpty()
            .Must(t => new[] { "STANDARD", "EXEMPT", "REDUCED" }.Contains(t))
            .WithMessage("TaxCategory must be STANDARD, EXEMPT or REDUCED.");
    }
}

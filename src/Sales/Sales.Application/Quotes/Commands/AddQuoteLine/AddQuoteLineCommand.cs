using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Commands.AddQuoteLine;

public record AddQuoteLineCommand(
    Guid QuoteId,
    Guid CatalogItemId,
    string SKU,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    string? Notes) : IRequest<QuoteDto>;

public class AddQuoteLineCommandValidator : AbstractValidator<AddQuoteLineCommand>
{
    public AddQuoteLineCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.SKU).NotEmpty();
        RuleFor(x => x.ItemName).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class AddQuoteLineCommandHandler : IRequestHandler<AddQuoteLineCommand, QuoteDto>
{
    private readonly IQuoteRepository _repo;
    private readonly IEventPublisher _events;

    public AddQuoteLineCommandHandler(IQuoteRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<QuoteDto> Handle(AddQuoteLineCommand cmd, CancellationToken ct)
    {
        var quote = await _repo.GetByIdAsync(cmd.QuoteId, ct)
            ?? throw new DomainException($"Quote '{cmd.QuoteId}' not found.");

        var line = QuoteLine.Create(cmd.CatalogItemId, cmd.SKU, cmd.ItemName,
            cmd.Quantity, Money.Of(cmd.UnitPrice, cmd.CurrencyCode),
            cmd.DiscountPercent, cmd.Notes);

        quote.AddLine(line);
        await _repo.UpdateAsync(quote, ct);

        return quote.ToDto();
    }
}

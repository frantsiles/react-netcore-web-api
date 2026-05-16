using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Commands.UpdateQuoteLine;

public record UpdateQuoteLineCommand(
    Guid QuoteId,
    Guid LineId,
    decimal Quantity,
    decimal UnitPrice,
    string CurrencyCode,
    decimal DiscountPercent,
    string? Notes) : IRequest<QuoteDto>;

public class UpdateQuoteLineCommandValidator : AbstractValidator<UpdateQuoteLineCommand>
{
    public UpdateQuoteLineCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class UpdateQuoteLineCommandHandler : IRequestHandler<UpdateQuoteLineCommand, QuoteDto>
{
    private readonly IQuoteRepository _repo;

    public UpdateQuoteLineCommandHandler(IQuoteRepository repo) => _repo = repo;

    public async Task<QuoteDto> Handle(UpdateQuoteLineCommand cmd, CancellationToken ct)
    {
        var quote = await _repo.GetByIdAsync(cmd.QuoteId, ct)
            ?? throw new DomainException($"Quote '{cmd.QuoteId}' not found.");

        quote.UpdateLine(cmd.LineId, cmd.Quantity,
            Money.Of(cmd.UnitPrice, cmd.CurrencyCode), cmd.DiscountPercent, cmd.Notes);

        await _repo.UpdateAsync(quote, ct);
        return quote.ToDto();
    }
}

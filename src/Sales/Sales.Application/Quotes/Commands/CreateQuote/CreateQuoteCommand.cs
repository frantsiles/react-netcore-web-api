using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Quotes;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Commands.CreateQuote;

public record CreateQuoteCommand(
    Guid CustomerId,
    DateOnly ValidUntil,
    string CurrencyCode,
    string CountryCode,
    string? Notes) : IRequest<QuoteDto>;

public class CreateQuoteCommandValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ValidUntil).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("ValidUntil must be today or a future date.");
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.CountryCode).NotEmpty().Length(2);
    }
}

public class CreateQuoteCommandHandler : IRequestHandler<CreateQuoteCommand, QuoteDto>
{
    private readonly IQuoteRepository _repo;
    private readonly IEventPublisher _events;

    public CreateQuoteCommandHandler(IQuoteRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<QuoteDto> Handle(CreateQuoteCommand cmd, CancellationToken ct)
    {
        var quoteNumber = GenerateNumber(cmd.CurrencyCode);
        var quote = Quote.Create(quoteNumber, cmd.CustomerId, cmd.ValidUntil,
            cmd.CurrencyCode, cmd.CountryCode, cmd.Notes);

        await _repo.AddAsync(quote, ct);
        foreach (var e in quote.DomainEvents)
            await _events.PublishAsync(e, ct);

        return quote.ToDto();
    }

    private static string GenerateNumber(string currencyCode) =>
        $"Q-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}

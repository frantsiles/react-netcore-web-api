using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Commands.SendQuote;

public record SendQuoteCommand(Guid QuoteId) : IRequest<QuoteDto>;

public class SendQuoteCommandValidator : AbstractValidator<SendQuoteCommand>
{
    public SendQuoteCommandValidator() => RuleFor(x => x.QuoteId).NotEmpty();
}

public class SendQuoteCommandHandler : IRequestHandler<SendQuoteCommand, QuoteDto>
{
    private readonly IQuoteRepository _repo;
    private readonly IEventPublisher _events;

    public SendQuoteCommandHandler(IQuoteRepository repo, IEventPublisher events)
        => (_repo, _events) = (repo, events);

    public async Task<QuoteDto> Handle(SendQuoteCommand cmd, CancellationToken ct)
    {
        var quote = await _repo.GetByIdAsync(cmd.QuoteId, ct)
            ?? throw new DomainException($"Quote '{cmd.QuoteId}' not found.");

        quote.Send();
        await _repo.UpdateAsync(quote, ct);
        foreach (var e in quote.DomainEvents)
            await _events.PublishAsync(e, ct);

        return quote.ToDto();
    }
}

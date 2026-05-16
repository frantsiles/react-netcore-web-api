using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Commands.RemoveQuoteLine;

public record RemoveQuoteLineCommand(Guid QuoteId, Guid LineId) : IRequest<QuoteDto>;

public class RemoveQuoteLineCommandValidator : AbstractValidator<RemoveQuoteLineCommand>
{
    public RemoveQuoteLineCommandValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}

public class RemoveQuoteLineCommandHandler : IRequestHandler<RemoveQuoteLineCommand, QuoteDto>
{
    private readonly IQuoteRepository _repo;

    public RemoveQuoteLineCommandHandler(IQuoteRepository repo) => _repo = repo;

    public async Task<QuoteDto> Handle(RemoveQuoteLineCommand cmd, CancellationToken ct)
    {
        var quote = await _repo.GetByIdAsync(cmd.QuoteId, ct)
            ?? throw new DomainException($"Quote '{cmd.QuoteId}' not found.");

        quote.RemoveLine(cmd.LineId);
        await _repo.UpdateAsync(quote, ct);
        return quote.ToDto();
    }
}

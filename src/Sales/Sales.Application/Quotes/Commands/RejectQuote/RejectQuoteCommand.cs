using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;
using Sales.Application.Common.Dtos;
using Sales.Application.Common.Mappings;
using Sales.Domain.Repositories;

namespace Sales.Application.Quotes.Commands.RejectQuote;

public record RejectQuoteCommand(Guid QuoteId) : IRequest<QuoteDto>;

public class RejectQuoteCommandValidator : AbstractValidator<RejectQuoteCommand>
{
    public RejectQuoteCommandValidator() => RuleFor(x => x.QuoteId).NotEmpty();
}

public class RejectQuoteCommandHandler : IRequestHandler<RejectQuoteCommand, QuoteDto>
{
    private readonly IQuoteRepository _repo;

    public RejectQuoteCommandHandler(IQuoteRepository repo) => _repo = repo;

    public async Task<QuoteDto> Handle(RejectQuoteCommand cmd, CancellationToken ct)
    {
        var quote = await _repo.GetByIdAsync(cmd.QuoteId, ct)
            ?? throw new DomainException($"Quote '{cmd.QuoteId}' not found.");

        quote.Reject();
        await _repo.UpdateAsync(quote, ct);
        return quote.ToDto();
    }
}

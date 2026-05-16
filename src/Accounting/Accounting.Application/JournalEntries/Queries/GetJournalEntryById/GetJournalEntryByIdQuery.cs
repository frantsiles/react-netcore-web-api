using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.Repositories;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.JournalEntries.Queries.GetJournalEntryById;

public record GetJournalEntryByIdQuery(Guid JournalEntryId) : IRequest<JournalEntryDto>;

public class GetJournalEntryByIdQueryValidator : AbstractValidator<GetJournalEntryByIdQuery>
{
    public GetJournalEntryByIdQueryValidator() => RuleFor(x => x.JournalEntryId).NotEmpty();
}

public class GetJournalEntryByIdQueryHandler : IRequestHandler<GetJournalEntryByIdQuery, JournalEntryDto>
{
    private readonly IJournalEntryRepository _repo;

    public GetJournalEntryByIdQueryHandler(IJournalEntryRepository repo) => _repo = repo;

    public async Task<JournalEntryDto> Handle(GetJournalEntryByIdQuery query, CancellationToken ct)
    {
        var entry = await _repo.GetByIdAsync(query.JournalEntryId, ct)
            ?? throw new DomainException($"Journal entry '{query.JournalEntryId}' not found.");
        return entry.ToDto();
    }
}

using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.JournalEntries;
using Accounting.Domain.Repositories;
using Api.Application.Common.Interfaces;
using MediatR;

namespace Accounting.Application.JournalEntries.Queries.SearchJournalEntries;

public record SearchJournalEntriesQuery(
    string? FiscalPeriod = null,
    EntryStatus? Status = null,
    Guid? AccountId = null,
    string? ReferenceType = null,
    Guid? ReferenceId = null) : IRequest<IReadOnlyList<JournalEntryDto>>;

public class SearchJournalEntriesQueryHandler
    : IRequestHandler<SearchJournalEntriesQuery, IReadOnlyList<JournalEntryDto>>
{
    private readonly IJournalEntryRepository _repo;
    private readonly ITenantContext _tenant;

    public SearchJournalEntriesQueryHandler(IJournalEntryRepository repo, ITenantContext tenant)
        => (_repo, _tenant) = (repo, tenant);

    public async Task<IReadOnlyList<JournalEntryDto>> Handle(
        SearchJournalEntriesQuery query, CancellationToken ct)
    {
        var entries = await _repo.SearchAsync(
            _tenant.TenantId, query.FiscalPeriod, query.Status,
            query.AccountId, query.ReferenceType, query.ReferenceId, ct);

        return entries.Select(e => e.ToDto()).ToList();
    }
}

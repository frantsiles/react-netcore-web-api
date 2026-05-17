using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Accounting;

public class ListAccountsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListAccountsBffQuery, IReadOnlyList<AccountBffDto>>
{
    public async Task<IReadOnlyList<AccountBffDto>> Handle(
        ListAccountsBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Type)) parts.Add($"type={request.Type}");
        if (request.IsActive.HasValue) parts.Add($"isActive={request.IsActive}");

        var url = parts.Count > 0
            ? $"api/accounting/accounts?{string.Join("&", parts)}"
            : "api/accounting/accounts";
        var result = await apiClient.GetAsync<List<AccountBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class SearchJournalEntriesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<SearchJournalEntriesBffQuery, IReadOnlyList<JournalEntryBffDto>>
{
    public async Task<IReadOnlyList<JournalEntryBffDto>> Handle(
        SearchJournalEntriesBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.FiscalPeriod)) parts.Add($"fiscalPeriod={Uri.EscapeDataString(request.FiscalPeriod)}");
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"status={request.Status}");
        if (request.AccountId.HasValue) parts.Add($"accountId={request.AccountId}");

        var url = parts.Count > 0
            ? $"api/accounting/journal-entries?{string.Join("&", parts)}"
            : "api/accounting/journal-entries";
        var result = await apiClient.GetAsync<List<JournalEntryBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class CreateAccountBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateAccountBffCommand, AccountBffDto>
{
    public async Task<AccountBffDto> Handle(CreateAccountBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            AccountNumber = request.AccountNumber,
            Name          = request.Name,
            Type          = request.Type,
            CurrencyCode  = request.CurrencyCode,
            Description   = request.Description,
        };
        var result = await apiClient.PostAsync<object, AccountBffDto>(
            "api/accounting/accounts", body, request.Token, ct);
        return result!;
    }
}

public class CreateJournalEntryBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateJournalEntryBffCommand, JournalEntryBffDto>
{
    public async Task<JournalEntryBffDto> Handle(
        CreateJournalEntryBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            FiscalPeriod  = request.FiscalPeriod,
            EntryDate     = request.EntryDate,
            Description   = request.Description,
            ReferenceType = request.ReferenceType,
            ReferenceId   = request.ReferenceId,
        };
        var result = await apiClient.PostAsync<object, JournalEntryBffDto>(
            "api/accounting/journal-entries", body, request.Token, ct);
        return result!;
    }
}

public class AddJeLineBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<AddJeLineBffCommand, JournalEntryBffDto>
{
    public async Task<JournalEntryBffDto> Handle(AddJeLineBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            AccountId   = request.AccountId,
            Side        = request.Side,
            Amount      = request.Amount,
            Description = request.Description,
        };
        var result = await apiClient.PostAsync<object, JournalEntryBffDto>(
            $"api/accounting/journal-entries/{request.EntryId}/lines", body, request.Token, ct);
        return result!;
    }
}

public class PostJournalEntryBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<PostJournalEntryBffCommand, JournalEntryBffDto>
{
    public async Task<JournalEntryBffDto> Handle(PostJournalEntryBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, JournalEntryBffDto>(
            $"api/accounting/journal-entries/{request.EntryId}/post", new { }, request.Token, ct);
        return result!;
    }
}

public class ReverseJournalEntryBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ReverseJournalEntryBffCommand, JournalEntryBffDto>
{
    public async Task<JournalEntryBffDto> Handle(
        ReverseJournalEntryBffCommand request, CancellationToken ct)
    {
        var body = new { ReversalDate = request.ReversalDate };
        var result = await apiClient.PostAsync<object, JournalEntryBffDto>(
            $"api/accounting/journal-entries/{request.EntryId}/reverse", body, request.Token, ct);
        return result!;
    }
}

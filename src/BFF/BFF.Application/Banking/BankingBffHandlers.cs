using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.Banking;

public class ListBankAccountsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListBankAccountsBffQuery, IReadOnlyList<BankAccountBffDto>>
{
    public async Task<IReadOnlyList<BankAccountBffDto>> Handle(
        ListBankAccountsBffQuery request, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(request.Status)
            ? "api/banking/accounts"
            : $"api/banking/accounts?status={request.Status}";
        var result = await apiClient.GetAsync<List<BankAccountBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class GetBankTransactionsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetBankTransactionsBffQuery, IReadOnlyList<BankTransactionBffDto>>
{
    public async Task<IReadOnlyList<BankTransactionBffDto>> Handle(
        GetBankTransactionsBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"status={request.Status}");
        if (request.From.HasValue) parts.Add($"from={request.From:O}");
        if (request.To.HasValue) parts.Add($"to={request.To:O}");

        var url = parts.Count > 0
            ? $"api/banking/accounts/{request.AccountId}/transactions?{string.Join("&", parts)}"
            : $"api/banking/accounts/{request.AccountId}/transactions";
        var result = await apiClient.GetAsync<List<BankTransactionBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class CreateBankAccountBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateBankAccountBffCommand, BankAccountBffDto>
{
    public async Task<BankAccountBffDto> Handle(CreateBankAccountBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            AccountNumber              = request.AccountNumber,
            BankName                   = request.BankName,
            CurrencyCode               = request.CurrencyCode,
            Iban                       = request.Iban,
            Swift                      = request.Swift,
            LinkedAccountingAccountId  = request.LinkedAccountingAccountId,
        };
        var result = await apiClient.PostAsync<object, BankAccountBffDto>(
            "api/banking/accounts", body, request.Token, ct);
        return result!;
    }
}

public class AddBankTransactionBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<AddBankTransactionBffCommand, BankTransactionBffDto>
{
    public async Task<BankTransactionBffDto> Handle(
        AddBankTransactionBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            TransactionDate = request.TransactionDate,
            Description     = request.Description,
            Amount          = request.Amount,
            Type            = request.Type,
            ReferenceNumber = request.ReferenceNumber,
        };
        var result = await apiClient.PostAsync<object, BankTransactionBffDto>(
            $"api/banking/accounts/{request.AccountId}/transactions", body, request.Token, ct);
        return result!;
    }
}

public class ReconcileTransactionBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<ReconcileTransactionBffCommand, BankTransactionBffDto>
{
    public async Task<BankTransactionBffDto> Handle(
        ReconcileTransactionBffCommand request, CancellationToken ct)
    {
        var body = new { JournalEntryId = request.JournalEntryId };
        var result = await apiClient.PostAsync<object, BankTransactionBffDto>(
            $"api/banking/accounts/{request.AccountId}/transactions/{request.TransactionId}/reconcile",
            body, request.Token, ct);
        return result!;
    }
}

public class UnreconcileBankTransactionBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<UnreconcileBankTransactionBffCommand, BankTransactionBffDto>
{
    public async Task<BankTransactionBffDto> Handle(
        UnreconcileBankTransactionBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, BankTransactionBffDto>(
            $"api/banking/accounts/{request.AccountId}/transactions/{request.TransactionId}/unreconcile",
            new { }, request.Token, ct);
        return result!;
    }
}

public class VoidBankTransactionBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<VoidBankTransactionBffCommand, BankTransactionBffDto>
{
    public async Task<BankTransactionBffDto> Handle(
        VoidBankTransactionBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, BankTransactionBffDto>(
            $"api/banking/accounts/{request.AccountId}/transactions/{request.TransactionId}/void",
            new { }, request.Token, ct);
        return result!;
    }
}

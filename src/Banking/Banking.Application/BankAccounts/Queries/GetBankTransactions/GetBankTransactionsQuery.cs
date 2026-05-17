using Api.Domain.Common;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.BankAccounts;
using Banking.Domain.Repositories;
using MediatR;

namespace Banking.Application.BankAccounts.Queries.GetBankTransactions;

public record GetBankTransactionsQuery(
    Guid BankAccountId,
    BankTransactionStatus? Status = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<IReadOnlyList<BankTransactionDto>>;

public class GetBankTransactionsQueryHandler
    : IRequestHandler<GetBankTransactionsQuery, IReadOnlyList<BankTransactionDto>>
{
    private readonly IBankAccountRepository _repo;

    public GetBankTransactionsQueryHandler(IBankAccountRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<BankTransactionDto>> Handle(
        GetBankTransactionsQuery query, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(query.BankAccountId, ct)
            ?? throw new DomainException($"Bank account '{query.BankAccountId}' not found.");

        var txns = account.Transactions.AsEnumerable();
        if (query.Status.HasValue)
            txns = txns.Where(t => t.Status == query.Status.Value);
        if (query.From.HasValue)
            txns = txns.Where(t => t.TransactionDate >= query.From.Value.Date);
        if (query.To.HasValue)
            txns = txns.Where(t => t.TransactionDate <= query.To.Value.Date);

        return txns.OrderByDescending(t => t.TransactionDate)
                   .Select(t => t.ToDto())
                   .ToList();
    }
}

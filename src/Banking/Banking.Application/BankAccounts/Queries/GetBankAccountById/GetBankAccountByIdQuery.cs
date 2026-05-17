using Api.Domain.Common;
using Banking.Application.Common.Dtos;
using Banking.Application.Common.Mappings;
using Banking.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Banking.Application.BankAccounts.Queries.GetBankAccountById;

public record GetBankAccountByIdQuery(Guid BankAccountId) : IRequest<BankAccountDto>;

public class GetBankAccountByIdQueryValidator : AbstractValidator<GetBankAccountByIdQuery>
{
    public GetBankAccountByIdQueryValidator() => RuleFor(x => x.BankAccountId).NotEmpty();
}

public class GetBankAccountByIdQueryHandler : IRequestHandler<GetBankAccountByIdQuery, BankAccountDto>
{
    private readonly IBankAccountRepository _repo;

    public GetBankAccountByIdQueryHandler(IBankAccountRepository repo) => _repo = repo;

    public async Task<BankAccountDto> Handle(GetBankAccountByIdQuery query, CancellationToken ct)
    {
        var account = await _repo.GetByIdAsync(query.BankAccountId, ct)
            ?? throw new DomainException($"Bank account '{query.BankAccountId}' not found.");
        return account.ToDto();
    }
}

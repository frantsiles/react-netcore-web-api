using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.JournalEntries;
using Accounting.Domain.Repositories;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.JournalEntries.Commands.AddJournalEntryLine;

public record AddJournalEntryLineCommand(
    Guid JournalEntryId,
    Guid AccountId,
    EntrySide Side,
    decimal Amount,
    string? Description = null) : IRequest<JournalEntryDto>;

public class AddJournalEntryLineCommandValidator : AbstractValidator<AddJournalEntryLineCommand>
{
    public AddJournalEntryLineCommandValidator()
    {
        RuleFor(x => x.JournalEntryId).NotEmpty();
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class AddJournalEntryLineCommandHandler : IRequestHandler<AddJournalEntryLineCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _jeRepo;
    private readonly IAccountRepository _accountRepo;

    public AddJournalEntryLineCommandHandler(IJournalEntryRepository jeRepo, IAccountRepository accountRepo)
        => (_jeRepo, _accountRepo) = (jeRepo, accountRepo);

    public async Task<JournalEntryDto> Handle(AddJournalEntryLineCommand cmd, CancellationToken ct)
    {
        var entry = await _jeRepo.GetByIdAsync(cmd.JournalEntryId, ct)
            ?? throw new DomainException($"Journal entry '{cmd.JournalEntryId}' not found.");

        var account = await _accountRepo.GetByIdAsync(cmd.AccountId, ct)
            ?? throw new DomainException($"Account '{cmd.AccountId}' not found.");

        if (!account.IsActive)
            throw new DomainException($"Account '{account.AccountNumber}' is inactive.");

        entry.AddLine(account.Id, account.AccountNumber, account.Name,
            cmd.Side, cmd.Amount, cmd.Description);

        await _jeRepo.UpdateAsync(entry, ct);
        return entry.ToDto();
    }
}

using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.JournalEntries;
using Accounting.Domain.Repositories;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.JournalEntries.Commands.ReverseJournalEntry;

public record ReverseJournalEntryCommand(
    Guid JournalEntryId,
    DateTime ReversalDate) : IRequest<JournalEntryDto>;

public class ReverseJournalEntryCommandValidator : AbstractValidator<ReverseJournalEntryCommand>
{
    public ReverseJournalEntryCommandValidator()
    {
        RuleFor(x => x.JournalEntryId).NotEmpty();
        RuleFor(x => x.ReversalDate).NotEmpty();
    }
}

public class ReverseJournalEntryCommandHandler : IRequestHandler<ReverseJournalEntryCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _jeRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IEventPublisher _events;

    public ReverseJournalEntryCommandHandler(
        IJournalEntryRepository jeRepo, IAccountRepository accountRepo, IEventPublisher events)
        => (_jeRepo, _accountRepo, _events) = (jeRepo, accountRepo, events);

    public async Task<JournalEntryDto> Handle(ReverseJournalEntryCommand cmd, CancellationToken ct)
    {
        var original = await _jeRepo.GetByIdAsync(cmd.JournalEntryId, ct)
            ?? throw new DomainException($"Journal entry '{cmd.JournalEntryId}' not found.");

        var reversalNumber = $"JE-REV-{cmd.ReversalDate:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var reversal = original.CreateReversal(reversalNumber, cmd.ReversalDate);

        // Post the reversal and update account balances
        var lines = reversal.Post();
        foreach (var line in lines)
        {
            var account = await _accountRepo.GetByIdAsync(line.AccountId, ct)
                ?? throw new DomainException($"Account '{line.AccountId}' not found.");

            if (line.Side == EntrySide.Debit)
                account.ApplyDebit(line.Amount);
            else
                account.ApplyCredit(line.Amount);

            await _accountRepo.UpdateAsync(account, ct);
        }

        await _jeRepo.UpdateAsync(original, ct);
        await _jeRepo.AddAsync(reversal, ct);

        foreach (var e in original.DomainEvents)
            await _events.PublishAsync(e, ct);
        foreach (var e in reversal.DomainEvents)
            await _events.PublishAsync(e, ct);

        return reversal.ToDto();
    }
}

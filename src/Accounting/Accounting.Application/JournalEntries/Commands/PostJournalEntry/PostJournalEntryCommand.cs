using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.JournalEntries;
using Accounting.Domain.Repositories;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.JournalEntries.Commands.PostJournalEntry;

public record PostJournalEntryCommand(Guid JournalEntryId) : IRequest<JournalEntryDto>;

public class PostJournalEntryCommandValidator : AbstractValidator<PostJournalEntryCommand>
{
    public PostJournalEntryCommandValidator() => RuleFor(x => x.JournalEntryId).NotEmpty();
}

public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _jeRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IEventPublisher _events;

    public PostJournalEntryCommandHandler(
        IJournalEntryRepository jeRepo, IAccountRepository accountRepo, IEventPublisher events)
        => (_jeRepo, _accountRepo, _events) = (jeRepo, accountRepo, events);

    public async Task<JournalEntryDto> Handle(PostJournalEntryCommand cmd, CancellationToken ct)
    {
        var entry = await _jeRepo.GetByIdAsync(cmd.JournalEntryId, ct)
            ?? throw new DomainException($"Journal entry '{cmd.JournalEntryId}' not found.");

        // Post returns lines so we can update account balances atomically
        var lines = entry.Post();

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

        await _jeRepo.UpdateAsync(entry, ct);
        foreach (var e in entry.DomainEvents)
            await _events.PublishAsync(e, ct);

        return entry.ToDto();
    }
}

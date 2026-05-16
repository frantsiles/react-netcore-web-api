using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.JournalEntries;
using Accounting.Domain.Repositories;
using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.JournalEntries.Commands.CreateJournalEntry;

public record CreateJournalEntryCommand(
    DateTime EntryDate,
    string Description,
    string? ReferenceType = null,
    Guid? ReferenceId = null) : IRequest<JournalEntryDto>;

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}

public class CreateJournalEntryCommandHandler : IRequestHandler<CreateJournalEntryCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _repo;
    private readonly ITenantContext _tenant;

    public CreateJournalEntryCommandHandler(IJournalEntryRepository repo, ITenantContext tenant)
        => (_repo, _tenant) = (repo, tenant);

    public async Task<JournalEntryDto> Handle(CreateJournalEntryCommand cmd, CancellationToken ct)
    {
        var entryNumber = $"JE-{cmd.EntryDate:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var entry = JournalEntry.Create(_tenant.TenantId, entryNumber, cmd.EntryDate,
            cmd.Description, cmd.ReferenceType, cmd.ReferenceId);

        await _repo.AddAsync(entry, ct);
        return entry.ToDto();
    }
}

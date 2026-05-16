using Accounting.Application.Common.Dtos;
using Accounting.Application.Common.Mappings;
using Accounting.Domain.Repositories;
using Api.Domain.Common;
using FluentValidation;
using MediatR;

namespace Accounting.Application.JournalEntries.Commands.RemoveJournalEntryLine;

public record RemoveJournalEntryLineCommand(Guid JournalEntryId, Guid LineId) : IRequest<JournalEntryDto>;

public class RemoveJournalEntryLineCommandValidator : AbstractValidator<RemoveJournalEntryLineCommand>
{
    public RemoveJournalEntryLineCommandValidator()
    {
        RuleFor(x => x.JournalEntryId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
    }
}

public class RemoveJournalEntryLineCommandHandler : IRequestHandler<RemoveJournalEntryLineCommand, JournalEntryDto>
{
    private readonly IJournalEntryRepository _repo;

    public RemoveJournalEntryLineCommandHandler(IJournalEntryRepository repo) => _repo = repo;

    public async Task<JournalEntryDto> Handle(RemoveJournalEntryLineCommand cmd, CancellationToken ct)
    {
        var entry = await _repo.GetByIdAsync(cmd.JournalEntryId, ct)
            ?? throw new DomainException($"Journal entry '{cmd.JournalEntryId}' not found.");

        entry.RemoveLine(cmd.LineId);
        await _repo.UpdateAsync(entry, ct);
        return entry.ToDto();
    }
}

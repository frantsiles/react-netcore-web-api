using Accounting.Application.Accounts.Commands.CreateAccount;
using Accounting.Application.Accounts.Commands.UpdateAccount;
using Accounting.Application.Accounts.Queries.GetAccountById;
using Accounting.Application.Accounts.Queries.ListAccounts;
using Accounting.Application.JournalEntries.Commands.AddJournalEntryLine;
using Accounting.Application.JournalEntries.Commands.CreateJournalEntry;
using Accounting.Application.JournalEntries.Commands.PostJournalEntry;
using Accounting.Application.JournalEntries.Commands.RemoveJournalEntryLine;
using Accounting.Application.JournalEntries.Commands.ReverseJournalEntry;
using Accounting.Application.JournalEntries.Queries.GetJournalEntryById;
using Accounting.Application.JournalEntries.Queries.SearchJournalEntries;
using Accounting.Domain.Accounts;
using Accounting.Domain.JournalEntries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/accounting/accounts")]
[Authorize]
public class AccountsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetAccountByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] AccountType? type,
        [FromQuery] bool? isActive,
        CancellationToken ct)
        => Ok(await sender.Send(new ListAccountsQuery(type, isActive), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateAccountCommand(id, req.Name, req.Description), ct));
}

[ApiController]
[Route("api/accounting/journal-entries")]
[Authorize]
public class JournalEntriesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJournalEntryCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetJournalEntryByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? fiscalPeriod,
        [FromQuery] EntryStatus? status,
        [FromQuery] Guid? accountId,
        [FromQuery] string? referenceType,
        [FromQuery] Guid? referenceId,
        CancellationToken ct)
        => Ok(await sender.Send(new SearchJournalEntriesQuery(fiscalPeriod, status, accountId, referenceType, referenceId), ct));

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] AddJeLineRequest req, CancellationToken ct)
        => Ok(await sender.Send(new AddJournalEntryLineCommand(id, req.AccountId, req.Side, req.Amount, req.Description), ct));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct)
        => Ok(await sender.Send(new RemoveJournalEntryLineCommand(id, lineId), ct));

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new PostJournalEntryCommand(id), ct));

    [HttpPost("{id:guid}/reverse")]
    public async Task<IActionResult> Reverse(Guid id, [FromBody] ReverseRequest req, CancellationToken ct)
        => Ok(await sender.Send(new ReverseJournalEntryCommand(id, req.ReversalDate), ct));
}

public record UpdateAccountRequest(string Name, string? Description);
public record AddJeLineRequest(Guid AccountId, EntrySide Side, decimal Amount, string? Description);
public record ReverseRequest(DateTime ReversalDate);

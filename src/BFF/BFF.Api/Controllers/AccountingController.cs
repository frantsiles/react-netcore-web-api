using BFF.Application.Accounting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/accounting/accounts")]
[Authorize]
public class AccountingAccountsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? type, [FromQuery] bool? isActive, CancellationToken ct)
        => Ok(await mediator.Send(new ListAccountsBffQuery(GetToken(), type, isActive), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateAccountBffCommand(
            GetToken(), req.AccountNumber, req.Name, req.Type, req.CurrencyCode, req.Description), ct);
        return Created(string.Empty, result);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

[ApiController]
[Route("bff/accounting/journal-entries")]
[Authorize]
public class AccountingJournalEntriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? fiscalPeriod,
        [FromQuery] string? status,
        [FromQuery] Guid? accountId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
        => Ok(await mediator.Send(
            new SearchJournalEntriesBffQuery(GetToken(), fiscalPeriod, status, accountId, skip, take), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJournalEntryBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateJournalEntryBffCommand(
            GetToken(), req.FiscalPeriod, req.EntryDate, req.Description,
            req.ReferenceType, req.ReferenceId), ct);
        return Created(string.Empty, result);
    }

    [HttpPost("{id:guid}/lines")]
    public async Task<IActionResult> AddLine(
        Guid id, [FromBody] AddJeLineBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new AddJeLineBffCommand(
            GetToken(), id, req.AccountId, req.Side, req.Amount, req.Description), ct));

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new PostJournalEntryBffCommand(GetToken(), id), ct));

    [HttpPost("{id:guid}/reverse")]
    public async Task<IActionResult> Reverse(
        Guid id, [FromBody] ReverseJeBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new ReverseJournalEntryBffCommand(GetToken(), id, req.ReversalDate), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreateAccountBffRequest(
    string AccountNumber, string Name, string Type, string CurrencyCode, string? Description);
public record CreateJournalEntryBffRequest(
    string FiscalPeriod, DateTime EntryDate, string Description,
    string? ReferenceType, Guid? ReferenceId);
public record AddJeLineBffRequest(Guid AccountId, string Side, decimal Amount, string? Description);
public record ReverseJeBffRequest(DateTime ReversalDate);

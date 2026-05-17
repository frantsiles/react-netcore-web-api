using BFF.Application.Banking;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/banking/accounts")]
[Authorize]
public class BankingController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
        => Ok(await mediator.Send(new ListBankAccountsBffQuery(GetToken(), status), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBankAccountBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateBankAccountBffCommand(
            GetToken(), req.AccountNumber, req.BankName, req.CurrencyCode,
            req.Iban, req.Swift, req.LinkedAccountingAccountId), ct);
        return Created(string.Empty, result);
    }

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid id,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
        => Ok(await mediator.Send(new GetBankTransactionsBffQuery(GetToken(), id, status, from, to), ct));

    [HttpPost("{id:guid}/transactions")]
    public async Task<IActionResult> AddTransaction(
        Guid id, [FromBody] AddBankTransactionBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new AddBankTransactionBffCommand(
            GetToken(), id, req.TransactionDate, req.Description, req.Amount, req.Type, req.ReferenceNumber), ct));

    [HttpPost("{id:guid}/transactions/{txId:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(
        Guid id, Guid txId, [FromBody] ReconcileBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new ReconcileTransactionBffCommand(
            GetToken(), id, txId, req.JournalEntryId), ct));

    [HttpPost("{id:guid}/transactions/{txId:guid}/unreconcile")]
    public async Task<IActionResult> Unreconcile(Guid id, Guid txId, CancellationToken ct)
        => Ok(await mediator.Send(new UnreconcileBankTransactionBffCommand(GetToken(), id, txId), ct));

    [HttpPost("{id:guid}/transactions/{txId:guid}/void")]
    public async Task<IActionResult> Void(Guid id, Guid txId, CancellationToken ct)
        => Ok(await mediator.Send(new VoidBankTransactionBffCommand(GetToken(), id, txId), ct));

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreateBankAccountBffRequest(
    string AccountNumber, string BankName, string CurrencyCode,
    string? Iban, string? Swift, Guid? LinkedAccountingAccountId);
public record AddBankTransactionBffRequest(
    DateTime TransactionDate, string Description, decimal Amount, string Type, string? ReferenceNumber);
public record ReconcileBffRequest(Guid JournalEntryId);

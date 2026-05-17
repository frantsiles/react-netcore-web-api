using Banking.Application.BankAccounts.Commands.AddBankTransaction;
using Banking.Application.BankAccounts.Commands.CreateBankAccount;
using Banking.Application.BankAccounts.Commands.ReconcileTransaction;
using Banking.Application.BankAccounts.Commands.VoidTransaction;
using Banking.Application.BankAccounts.Queries.GetBankAccountById;
using Banking.Application.BankAccounts.Queries.GetBankTransactions;
using Banking.Application.BankAccounts.Queries.ListBankAccounts;
using Banking.Domain.BankAccounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/banking/accounts")]
[Authorize]
public class BankAccountsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBankAccountCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetBankAccountByIdQuery(id), ct));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] BankAccountStatus? status, CancellationToken ct)
        => Ok(await sender.Send(new ListBankAccountsQuery(status), ct));

    [HttpGet("{id:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid id,
        [FromQuery] BankTransactionStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
        => Ok(await sender.Send(new GetBankTransactionsQuery(id, status, from, to), ct));

    [HttpPost("{id:guid}/transactions")]
    public async Task<IActionResult> AddTransaction(
        Guid id, [FromBody] AddTransactionRequest req, CancellationToken ct)
        => Ok(await sender.Send(new AddBankTransactionCommand(
            id, req.TransactionDate, req.Description, req.Amount, req.Type, req.ReferenceNumber), ct));

    [HttpPost("{id:guid}/transactions/{txId:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(
        Guid id, Guid txId, [FromBody] ReconcileRequest req, CancellationToken ct)
        => Ok(await sender.Send(new ReconcileTransactionCommand(id, txId, req.JournalEntryId), ct));

    [HttpPost("{id:guid}/transactions/{txId:guid}/void")]
    public async Task<IActionResult> Void(Guid id, Guid txId, CancellationToken ct)
        => Ok(await sender.Send(new VoidTransactionCommand(id, txId), ct));
}

public record AddTransactionRequest(
    DateTime TransactionDate, string Description,
    decimal Amount, BankTransactionType Type, string? ReferenceNumber);
public record ReconcileRequest(Guid JournalEntryId);

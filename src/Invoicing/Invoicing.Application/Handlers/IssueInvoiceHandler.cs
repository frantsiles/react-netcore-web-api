using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using Invoicing.Application.Commands;
using Invoicing.Application.DTOs;
using Invoicing.Domain.Repositories;
using MediatR;

namespace Invoicing.Application.Handlers;

public class IssueInvoiceHandler(IInvoiceRepository repo, IEventPublisher events)
    : IRequestHandler<IssueInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(IssueInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdAsync(cmd.InvoiceId, ct)
            ?? throw new DomainException($"Invoice '{cmd.InvoiceId}' not found.");

        invoice.Issue();
        await repo.UpdateAsync(invoice, ct);

        foreach (var e in invoice.DomainEvents)
            await events.PublishAsync(e, ct);

        return invoice.ToDto();
    }
}

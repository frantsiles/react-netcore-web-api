using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Invoicing.Application.Commands;
using Invoicing.Application.DTOs;
using Invoicing.Domain.Repositories;
using MediatR;

namespace Invoicing.Application.Handlers;

public class CancelInvoiceCommandValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CancelInvoiceHandler(IInvoiceRepository repo, IEventPublisher events)
    : IRequestHandler<CancelInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(CancelInvoiceCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdAsync(cmd.InvoiceId, ct)
            ?? throw new DomainException($"Invoice '{cmd.InvoiceId}' not found.");

        invoice.Cancel(cmd.Reason);
        await repo.UpdateAsync(invoice, ct);

        foreach (var e in invoice.DomainEvents)
            await events.PublishAsync(e, ct);

        return invoice.ToDto();
    }
}

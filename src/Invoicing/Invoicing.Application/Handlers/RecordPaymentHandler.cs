using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Invoicing.Application.Commands;
using Invoicing.Application.DTOs;
using Invoicing.Domain.Repositories;
using MediatR;

namespace Invoicing.Application.Handlers;

public class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).Length(3);
    }
}

public class RecordPaymentHandler(IInvoiceRepository repo, IEventPublisher events)
    : IRequestHandler<RecordPaymentCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(RecordPaymentCommand cmd, CancellationToken ct)
    {
        var invoice = await repo.GetByIdAsync(cmd.InvoiceId, ct)
            ?? throw new DomainException($"Invoice '{cmd.InvoiceId}' not found.");

        invoice.RecordPayment(Money.Of(cmd.Amount, cmd.CurrencyCode), cmd.PaidAt, cmd.Reference);
        await repo.UpdateAsync(invoice, ct);

        foreach (var e in invoice.DomainEvents)
            await events.PublishAsync(e, ct);

        return invoice.ToDto();
    }
}

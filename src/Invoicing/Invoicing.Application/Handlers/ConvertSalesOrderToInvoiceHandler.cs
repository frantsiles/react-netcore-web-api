using Api.Application.Common.Interfaces;
using Api.Domain.Common;
using FluentValidation;
using Invoicing.Application.Commands;
using Invoicing.Application.DTOs;
using Invoicing.Domain.Invoices;
using Invoicing.Domain.Repositories;
using MediatR;
using Sales.Domain.Repositories;

namespace Invoicing.Application.Handlers;

public class ConvertSalesOrderToInvoiceCommandValidator : AbstractValidator<ConvertSalesOrderToInvoiceCommand>
{
    public ConvertSalesOrderToInvoiceCommandValidator()
    {
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.DueDate).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("DueDate must be today or in the future.");
    }
}

public class ConvertSalesOrderToInvoiceHandler(
    ISalesOrderRepository salesOrderRepo,
    IInvoiceRepository invoiceRepo,
    IEventPublisher events)
    : IRequestHandler<ConvertSalesOrderToInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(ConvertSalesOrderToInvoiceCommand cmd, CancellationToken ct)
    {
        var order = await salesOrderRepo.GetByIdAsync(cmd.SalesOrderId, ct)
            ?? throw new DomainException($"Sales order '{cmd.SalesOrderId}' not found.");

        if (order.Status != Sales.Domain.Orders.SalesOrderStatus.Confirmed
            && order.Status != Sales.Domain.Orders.SalesOrderStatus.PartiallyFulfilled
            && order.Status != Sales.Domain.Orders.SalesOrderStatus.Fulfilled)
            throw new DomainException($"Sales order '{order.OrderNumber}' must be Confirmed, PartiallyFulfilled or Fulfilled to convert to invoice. Current status: {order.Status}.");

        var invoiceNumber = GenerateInvoiceNumber();
        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var invoice = Invoice.Create(invoiceNumber, order.CustomerId, order.CurrencyCode,
            issueDate, cmd.DueDate, order.Id, cmd.Notes);

        foreach (var line in order.Lines)
        {
            var invLine = InvoiceLine.Create(
                line.ItemName,
                line.Quantity,
                line.UnitPrice,
                line.DiscountPercent,
                line.CatalogItemId,
                line.SKU,
                line.Id);
            invoice.AddLine(invLine);
        }

        invoice.Issue();
        order.MarkInvoiced();

        await invoiceRepo.AddAsync(invoice, ct);
        await salesOrderRepo.UpdateAsync(order, ct);

        foreach (var e in invoice.DomainEvents.Concat(order.DomainEvents))
            await events.PublishAsync(e, ct);

        return invoice.ToDto();
    }

    private static string GenerateInvoiceNumber() =>
        $"INV-{DateTime.UtcNow:yyyyMM}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}

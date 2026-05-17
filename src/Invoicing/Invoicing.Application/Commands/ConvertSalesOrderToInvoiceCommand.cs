using Invoicing.Application.DTOs;
using MediatR;

namespace Invoicing.Application.Commands;

public record ConvertSalesOrderToInvoiceCommand(
    Guid SalesOrderId,
    DateOnly DueDate,
    string? Notes = null) : IRequest<InvoiceDto>;

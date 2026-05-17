using Invoicing.Application.DTOs;
using Invoicing.Domain.Invoices;
using MediatR;

namespace Invoicing.Application.Queries;

public record SearchInvoicesQuery(
    Guid? CustomerId,
    InvoiceStatus? Status,
    Guid? OriginSalesOrderId,
    int Skip = 0,
    int Take = 50) : IRequest<IReadOnlyList<InvoiceDto>>;

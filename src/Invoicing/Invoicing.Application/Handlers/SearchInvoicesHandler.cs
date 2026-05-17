using Invoicing.Application.DTOs;
using Invoicing.Application.Queries;
using Invoicing.Domain.Repositories;
using MediatR;

namespace Invoicing.Application.Handlers;

public class SearchInvoicesHandler(IInvoiceRepository repo)
    : IRequestHandler<SearchInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(SearchInvoicesQuery query, CancellationToken ct)
    {
        var invoices = await repo.SearchAsync(
            query.CustomerId, query.Status, query.OriginSalesOrderId,
            query.Skip, query.Take, ct);
        return invoices.Select(i => i.ToDto()).ToList();
    }
}

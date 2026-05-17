using Invoicing.Application.DTOs;
using Invoicing.Application.Queries;
using Invoicing.Domain.Repositories;
using MediatR;

namespace Invoicing.Application.Handlers;

public class GetInvoiceByIdHandler(IInvoiceRepository repo)
    : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto?>
{
    public async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery query, CancellationToken ct)
    {
        var invoice = await repo.GetByIdAsync(query.InvoiceId, ct);
        return invoice?.ToDto();
    }
}

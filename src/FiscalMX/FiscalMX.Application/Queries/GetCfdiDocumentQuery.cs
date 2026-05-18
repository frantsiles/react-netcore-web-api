using FiscalMX.Application.Commands;
using FiscalMX.Application.DTOs;
using FiscalMX.Domain.Services;
using MediatR;

namespace FiscalMX.Application.Queries;

public record GetCfdiDocumentQuery(Guid InvoiceId) : IRequest<CfdiDocumentDto?>;

public class GetCfdiDocumentHandler(ICfdiDocumentRepository repo)
    : IRequestHandler<GetCfdiDocumentQuery, CfdiDocumentDto?>
{
    public async Task<CfdiDocumentDto?> Handle(GetCfdiDocumentQuery req, CancellationToken ct)
    {
        var doc = await repo.FindByInvoiceIdAsync(req.InvoiceId, ct);
        return doc is null ? null : TimbrarCfdiHandler.ToDto(doc);
    }
}

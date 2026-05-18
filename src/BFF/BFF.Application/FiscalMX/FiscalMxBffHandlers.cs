using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.FiscalMX;

public class TimbrarCfdiBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<TimbrarCfdiBffCommand, CfdiDocumentBffDto>
{
    public async Task<CfdiDocumentBffDto> Handle(
        TimbrarCfdiBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, CfdiDocumentBffDto>(
            $"api/fiscal/mx/invoices/{request.InvoiceId}/timbrar",
            new { }, request.Token, ct);
        return result!;
    }
}

public class GetCfdiDocumentBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetCfdiDocumentBffQuery, CfdiDocumentBffDto?>
{
    public Task<CfdiDocumentBffDto?> Handle(
        GetCfdiDocumentBffQuery request, CancellationToken ct)
        => apiClient.GetAsync<CfdiDocumentBffDto>(
            $"api/fiscal/mx/invoices/{request.InvoiceId}", request.Token, ct);
}

public class CancelarCfdiBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CancelarCfdiBffCommand, CfdiDocumentBffDto>
{
    public async Task<CfdiDocumentBffDto> Handle(
        CancelarCfdiBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, CfdiDocumentBffDto>(
            $"api/fiscal/mx/invoices/{request.InvoiceId}/cancelar",
            new { request.Motivo, request.UuidRelacionado }, request.Token, ct);
        return result!;
    }
}

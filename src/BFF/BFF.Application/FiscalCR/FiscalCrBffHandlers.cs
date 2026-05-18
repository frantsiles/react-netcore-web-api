using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.FiscalCR;

public class TimbrarFacturaBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<TimbrarFacturaBffCommand, ElectronicDocumentBffDto>
{
    public async Task<ElectronicDocumentBffDto> Handle(
        TimbrarFacturaBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, ElectronicDocumentBffDto>(
            $"api/fiscal/cr/invoices/{request.InvoiceId}/timbre",
            new { }, request.Token, ct);
        return result!;
    }
}

public class GetElectronicDocumentBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<GetElectronicDocumentBffQuery, ElectronicDocumentBffDto?>
{
    public Task<ElectronicDocumentBffDto?> Handle(
        GetElectronicDocumentBffQuery request, CancellationToken ct)
        => apiClient.GetAsync<ElectronicDocumentBffDto>(
            $"api/fiscal/cr/invoices/{request.InvoiceId}", request.Token, ct);
}

public class PollDocumentStatusBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<PollDocumentStatusBffCommand, ElectronicDocumentBffDto>
{
    public async Task<ElectronicDocumentBffDto> Handle(
        PollDocumentStatusBffCommand request, CancellationToken ct)
    {
        var result = await apiClient.PostAsync<object, ElectronicDocumentBffDto>(
            $"api/fiscal/cr/invoices/{request.InvoiceId}/poll",
            new { }, request.Token, ct);
        return result!;
    }
}

using FiscalCR.Domain.Services;
using Microsoft.Extensions.Logging;

namespace FiscalCR.Infrastructure.Services;

/// <summary>
/// Development stub: simulates Hacienda ATV accepting the document.
/// Replace with RealHaciendaClient when ATV credentials are configured.
/// Real API: POST https://api-sandbox.comprobanteselectronicos.go.cr/recepcion/v1/recepcion
/// </summary>
public class StubHaciendaClient(ILogger<StubHaciendaClient> logger) : IHaciendaClient
{
    private readonly Dictionary<string, HaciendaStatusResponse> _submitted = [];

    public Task<bool> SubmitAsync(HaciendaSubmitRequest request, Guid tenantId, CancellationToken ct = default)
    {
        logger.LogInformation(
            "StubHaciendaClient: simulating submission of clave {Clave} for tenant {TenantId}",
            request.Clave, tenantId);

        _submitted[request.Clave] = new HaciendaStatusResponse(
            request.Clave, "aceptado",
            "Aceptado por Hacienda (ambiente de desarrollo)",
            null);

        return Task.FromResult(true);
    }

    public Task<HaciendaStatusResponse?> QueryStatusAsync(string clave, Guid tenantId, CancellationToken ct = default)
    {
        _submitted.TryGetValue(clave, out var response);
        return Task.FromResult<HaciendaStatusResponse?>(
            response ?? new HaciendaStatusResponse(clave, "aceptado",
                "Aceptado por Hacienda (ambiente de desarrollo)", null));
    }
}

namespace FiscalCR.Domain.Services;

public record HaciendaSubmitRequest(
    string Clave,
    string Fecha,                 // ISO8601 with CR offset
    string EmisorTipoId,
    string EmisorCedula,
    string? ReceptorTipoId,
    string? ReceptorCedula,
    string ComprobanteXmlBase64); // Base64 of signed XML

public record HaciendaStatusResponse(
    string Clave,
    string IndEstado,             // "aceptado" | "rechazado" | "procesando"
    string? Mensaje,
    string? XmlRespuesta);

/// <summary>
/// Communicates with the Hacienda ATV API.
/// Sandbox: https://api-sandbox.comprobanteselectronicos.go.cr/recepcion/v1/
/// Production: https://api.comprobanteselectronicos.go.cr/recepcion/v1/
/// </summary>
public interface IHaciendaClient
{
    Task<bool> SubmitAsync(HaciendaSubmitRequest request, Guid tenantId, CancellationToken ct = default);
    Task<HaciendaStatusResponse?> QueryStatusAsync(string clave, Guid tenantId, CancellationToken ct = default);
}

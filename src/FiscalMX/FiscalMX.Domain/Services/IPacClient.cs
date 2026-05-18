namespace FiscalMX.Domain.Services;

public record PacTimbraRequest(string XmlOriginal, Guid TenantId);
public record PacTimbraResponse(bool Success, string? Uuid, string? XmlTimbrado, string? ErrorMessage);

public record PacCancelRequest(string Uuid, string EmisorRfc, string Motivo, Guid TenantId, string? UuidRelacionado = null);
public record PacCancelResponse(bool Success, string? AcuseXml, string? ErrorMessage);

public interface IPacClient
{
    Task<PacTimbraResponse> TimbrarAsync(PacTimbraRequest request, CancellationToken ct = default);
    Task<PacCancelResponse> CancelarAsync(PacCancelRequest request, CancellationToken ct = default);
}

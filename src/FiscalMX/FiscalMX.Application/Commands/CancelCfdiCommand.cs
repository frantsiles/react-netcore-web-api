using FiscalMX.Application.Commands;
using FiscalMX.Application.DTOs;
using FiscalMX.Domain.CfdiDocuments;
using FiscalMX.Domain.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FiscalMX.Application.Commands;

public record CancelCfdiCommand(Guid InvoiceId, string Motivo, string? UuidRelacionado = null)
    : IRequest<CfdiDocumentDto>;

public class CancelCfdiHandler(
    ICfdiDocumentRepository repo,
    IPacClient pacClient,
    IConfiguration config,
    ILogger<CancelCfdiHandler> logger)
    : IRequestHandler<CancelCfdiCommand, CfdiDocumentDto>
{
    public async Task<CfdiDocumentDto> Handle(CancelCfdiCommand req, CancellationToken ct)
    {
        var doc = await repo.FindByInvoiceIdAsync(req.InvoiceId, ct)
            ?? throw new InvalidOperationException("No existe CFDI para esta factura.");

        if (doc.Status != CfdiStatus.Timbrado)
            throw new InvalidOperationException("Solo se puede cancelar un CFDI timbrado.");

        var emisorRfc = config["FiscalMX:EmisorRfc"] ?? "AAA010101AAA";

        try
        {
            var cancelResp = await pacClient.CancelarAsync(
                new PacCancelRequest(doc.Uuid!, emisorRfc, req.Motivo, doc.TenantId, req.UuidRelacionado), ct);

            if (cancelResp.Success)
                doc.MarkCancelled(req.Motivo);
            else
                doc.MarkError(cancelResp.ErrorMessage ?? "Error al cancelar");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelando CFDI {Uuid}", doc.Uuid);
            doc.MarkError(ex.Message);
        }

        await repo.SaveChangesAsync(ct);
        return TimbrarCfdiHandler.ToDto(doc);
    }
}

using Api.Domain.Common;

namespace FiscalMX.Domain.CfdiDocuments;

/// <summary>
/// Tracks the CFDI 4.0 lifecycle for a Mexican invoice.
/// One-to-one with an Invoice; stores the PAC UUID, signed XML, and SAT/PAC response.
/// </summary>
public class CfdiDocument : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; private set; }
    public string InvoiceNumber { get; private set; } = "";

    // UUID assigned by the PAC after timbrado (RFC 4122)
    public string? Uuid { get; private set; }
    public string? Serie { get; private set; }
    public string? Folio { get; private set; }

    public CfdiStatus Status { get; private set; }

    public string? XmlOriginal { get; private set; }   // unsigned
    public string? XmlTimbrado { get; private set; }   // with TFD complement

    // PAC / SAT response
    public string? PacMensaje { get; private set; }
    public string? CancelMotivo { get; private set; }  // motivo de cancelación

    public new DateTime CreatedAt { get; private set; }
    public DateTime? TimbradoAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private CfdiDocument() : base() { }

    public static CfdiDocument Create(
        Guid tenantId, Guid invoiceId, string invoiceNumber,
        string? serie, string? folio)
        => new()
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            Serie = serie,
            Folio = folio,
            Status = CfdiStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

    public void SetOriginalXml(string xml) => XmlOriginal = xml;

    public void MarkTimbrado(string uuid, string xmlTimbrado)
    {
        Uuid = uuid;
        XmlTimbrado = xmlTimbrado;
        Status = CfdiStatus.Timbrado;
        TimbradoAt = DateTime.UtcNow;
    }

    public void MarkCancelled(string motivo)
    {
        CancelMotivo = motivo;
        Status = CfdiStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void MarkError(string message)
    {
        PacMensaje = message;
        Status = CfdiStatus.Error;
    }
}

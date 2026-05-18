using Api.Domain.Common;

namespace FiscalCR.Domain.ElectronicDocuments;

/// <summary>
/// Tracks the lifecycle of a Costa Rica electronic invoice submitted to Hacienda ATV.
/// One-to-one with an Invoice; stores the 50-digit clave, signed XML, and Hacienda response.
/// </summary>
public class ElectronicDocument : AggregateRoot, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; private set; }
    public string InvoiceNumber { get; private set; } = "";

    // CR electronic invoice key — 50 digits generated at timbre time
    public string Clave { get; private set; } = "";
    public string NumeroConsecutivo { get; private set; } = "";

    public ElectronicDocumentStatus Status { get; private set; }

    // Populated after XML generation + signing
    public string? XmlUnsigned { get; private set; }
    public string? XmlSigned { get; private set; }

    // Populated after Hacienda response
    public string? HaciendaMensaje { get; private set; }
    public string? HaciendaXmlRespuesta { get; private set; }
    public string? HaciendaEstado { get; private set; } // "aceptado" | "rechazado" | "procesando"

    public new DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }

    private ElectronicDocument() : base() { }

    public static ElectronicDocument Create(
        Guid tenantId, Guid invoiceId, string invoiceNumber,
        string clave, string numeroConsecutivo)
    {
        return new ElectronicDocument
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            Clave = clave,
            NumeroConsecutivo = numeroConsecutivo,
            Status = ElectronicDocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void SetUnsignedXml(string xml)
    {
        XmlUnsigned = xml;
    }

    public void MarkSigned(string signedXml)
    {
        XmlSigned = signedXml;
        Status = ElectronicDocumentStatus.Signed;
    }

    public void MarkSubmitted()
    {
        Status = ElectronicDocumentStatus.Submitted;
        SubmittedAt = DateTime.UtcNow;
    }

    public void MarkAccepted(string mensaje, string? xmlRespuesta)
    {
        Status = ElectronicDocumentStatus.Accepted;
        HaciendaEstado = "aceptado";
        HaciendaMensaje = mensaje;
        HaciendaXmlRespuesta = xmlRespuesta;
        AcknowledgedAt = DateTime.UtcNow;
    }

    public void MarkRejected(string mensaje, string? xmlRespuesta)
    {
        Status = ElectronicDocumentStatus.Rejected;
        HaciendaEstado = "rechazado";
        HaciendaMensaje = mensaje;
        HaciendaXmlRespuesta = xmlRespuesta;
        AcknowledgedAt = DateTime.UtcNow;
    }

    public void MarkError(string errorMessage)
    {
        Status = ElectronicDocumentStatus.Error;
        HaciendaMensaje = errorMessage;
    }
}

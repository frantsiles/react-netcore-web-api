namespace FiscalCR.Domain.ElectronicDocuments;

public enum ElectronicDocumentStatus
{
    Pending,    // document created, not yet signed
    Signed,     // XML signed with tenant certificate
    Submitted,  // sent to Hacienda, waiting for response
    Accepted,   // Hacienda accepted (aceptado)
    Rejected,   // Hacienda rejected (rechazado) — correctable
    Error,      // system error during processing
}

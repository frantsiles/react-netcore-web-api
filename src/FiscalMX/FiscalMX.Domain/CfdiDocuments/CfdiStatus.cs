namespace FiscalMX.Domain.CfdiDocuments;

public enum CfdiStatus
{
    Pending,    // XML generado, pendiente de timbrado
    Timbrado,   // UUID asignado por el PAC
    Cancelled,  // Cancelado ante el SAT
    Error       // Error en algún paso
}

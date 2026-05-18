namespace FiscalCR.Domain.Services;

/// <summary>Input data needed to generate a CR FE XML document.</summary>
public record FeCrInvoiceData(
    string Clave,
    string NumeroConsecutivo,
    DateTime FechaEmision,
    string CodigoActividad,        // economic activity code
    // Emisor
    string EmisorNombre,
    string EmisorTipoId,           // 01=física 02=jurídica 03=DIMEX 04=NITE
    string EmisorCedula,
    string EmisorNombreComercial,
    string EmisorProvincia,
    string EmisorCanton,
    string EmisorDistrito,
    string EmisorOtrasSenas,
    string? EmisorTelefono,
    string EmisorEmail,
    // Receptor
    string? ReceptorNombre,
    string? ReceptorTipoId,
    string? ReceptorCedula,
    string? ReceptorEmail,
    // Condición venta
    string CondicionVenta,         // 01=contado
    string MedioPago,              // 01=efectivo 02=tarjeta 04=transferencia
    string? PlazoCredito,
    // Lines
    IReadOnlyList<FeCrLineData> Lines,
    // Currency
    string CodigoMoneda,
    decimal TipoCambio);

public record FeCrLineData(
    int NumeroLinea,
    string Codigo,
    string Detalle,
    decimal Cantidad,
    string UnidadMedida,           // Unid, kg, m2, etc.
    decimal PrecioUnitario,
    decimal MontoDescuento,
    decimal SubTotal,
    decimal MontoImpuesto,
    decimal TarifaImpuesto,        // 13, 4, 2, 1, or 0
    string CodigoImpuesto,         // 01=IVA
    string CodigoTarifaImpuesto,   // 08=13% 04=4% 02=2% 01=1% 07=exento
    decimal MontoTotalLinea);

public interface IFeCrXmlGenerator
{
    string Generate(FeCrInvoiceData data);
}

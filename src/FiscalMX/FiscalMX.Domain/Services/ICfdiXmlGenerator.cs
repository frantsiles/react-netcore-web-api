namespace FiscalMX.Domain.Services;

public record CfdiLineData(
    string ClaveProdServ,
    string ClaveUnidad,
    string NoIdentificacion,
    string Descripcion,
    decimal Cantidad,
    decimal ValorUnitario,
    decimal Importe,
    decimal DescuentoImporte,
    decimal TrasladoBase,
    decimal TrasladoTasa,   // 0.16 for IVA 16%, 0.00 for exento
    decimal TrasladoImporte,
    string ObjetoImp);      // "01"=no objeto, "02"=sí objeto, "03"=sí objeto no obliga

public record CfdiInvoiceData(
    string EmisorRfc,
    string EmisorNombre,
    string EmisorRegimenFiscal,
    string LugarExpedicion,
    string ReceptorRfc,
    string ReceptorNombre,
    string ReceptorDomicilioFiscal,
    string ReceptorRegimenFiscal,
    string ReceptorUsoCfdi,
    string? Serie,
    string? Folio,
    DateTime Fecha,
    string FormaPago,       // c_FormaPago: 01=Efectivo, 03=Transferencia, 99=Por definir
    string MetodoPago,      // PUE=pago único, PPD=pago en parcialidades
    string Moneda,
    decimal TipoCambio,
    string TipoDeComprobante, // I=Ingreso, E=Egreso, T=Traslado, P=Pago
    string Exportacion,     // 01=No aplica
    decimal Subtotal,
    decimal Descuento,
    decimal TotalImpuestos,
    decimal Total,
    IReadOnlyList<CfdiLineData> Lines);

public interface ICfdiXmlGenerator
{
    string Generate(CfdiInvoiceData data);
}

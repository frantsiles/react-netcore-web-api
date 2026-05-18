using FiscalCR.Domain.Services;
using System.Xml;
using System.Xml.Linq;

namespace FiscalCR.Infrastructure.Services;

/// <summary>
/// Generates a Costa Rica FacturaElectronica v4.3 XML document.
/// Namespace: https://cdn.comprobanteselectronicos.go.cr/xml-schemas/v4.3/facturaElectronica
/// </summary>
public class FeCrXmlGenerator : IFeCrXmlGenerator
{
    private static readonly XNamespace Ns =
        "https://cdn.comprobanteselectronicos.go.cr/xml-schemas/v4.3/facturaElectronica";

    public string Generate(FeCrInvoiceData d)
    {
        var totalSubtotal   = d.Lines.Sum(l => l.SubTotal);
        var totalImpuesto   = d.Lines.Sum(l => l.MontoImpuesto);
        var totalDescuento  = d.Lines.Sum(l => l.MontoDescuento);
        var totalGravado    = d.Lines.Where(l => l.TarifaImpuesto > 0).Sum(l => l.SubTotal);
        var totalExento     = d.Lines.Where(l => l.TarifaImpuesto == 0).Sum(l => l.SubTotal);
        var totalVenta      = totalSubtotal + totalDescuento;
        var totalComprobante = totalSubtotal + totalImpuesto;

        var cr = TimeZoneInfo.FindSystemTimeZoneById("America/Costa_Rica");
        var fechaCr = TimeZoneInfo.ConvertTimeFromUtc(d.FechaEmision.ToUniversalTime(), cr);
        var fechaStr = fechaCr.ToString("yyyy-MM-ddTHH:mm:ss") + "-06:00";

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "FacturaElectronica",
                new XAttribute("xmlns", Ns.NamespaceName),
                Elem("Clave", d.Clave),
                Elem("CodigoActividad", d.CodigoActividad),
                Elem("NumeroConsecutivo", d.NumeroConsecutivo),
                Elem("FechaEmision", fechaStr),
                BuildEmisor(d),
                BuildReceptor(d),
                Elem("CondicionVenta", d.CondicionVenta),
                d.PlazoCredito is not null ? Elem("PlazoCredito", d.PlazoCredito) : null!,
                Elem("MedioPago", d.MedioPago),
                BuildDetalles(d.Lines),
                BuildResumen(d, totalGravado, totalExento, totalVenta,
                             totalDescuento, totalImpuesto, totalComprobante)));

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = System.Text.Encoding.UTF8,
        };

        using var sw = new System.IO.StringWriter();
        using var xw = XmlWriter.Create(sw, settings);
        doc.WriteTo(xw);
        xw.Flush();
        return sw.ToString();
    }

    private static XElement BuildEmisor(FeCrInvoiceData d) =>
        new(Ns + "Emisor",
            Elem("Nombre", d.EmisorNombre),
            new XElement(Ns + "Identificacion",
                Elem("Tipo", d.EmisorTipoId),
                Elem("Numero", d.EmisorCedula.PadLeft(12, '0'))),
            Elem("NombreComercial", d.EmisorNombreComercial),
            new XElement(Ns + "Ubicacion",
                Elem("Provincia", d.EmisorProvincia),
                Elem("Canton", d.EmisorCanton),
                Elem("Distrito", d.EmisorDistrito),
                Elem("OtrasSenas", d.EmisorOtrasSenas)),
            d.EmisorTelefono is not null
                ? new XElement(Ns + "Telefono",
                    Elem("CodigoPais", "506"),
                    Elem("NumTelefono", d.EmisorTelefono.Replace("+506", "").Trim()))
                : null!,
            Elem("CorreoElectronico", d.EmisorEmail));

    private static XElement? BuildReceptor(FeCrInvoiceData d)
    {
        if (d.ReceptorNombre is null) return null;
        var r = new XElement(Ns + "Receptor",
            Elem("Nombre", d.ReceptorNombre));
        if (d.ReceptorTipoId is not null && d.ReceptorCedula is not null)
            r.Add(new XElement(Ns + "Identificacion",
                Elem("Tipo", d.ReceptorTipoId),
                Elem("Numero", d.ReceptorCedula)));
        if (d.ReceptorEmail is not null)
            r.Add(Elem("CorreoElectronico", d.ReceptorEmail));
        return r;
    }

    private static XElement BuildDetalles(IReadOnlyList<FeCrLineData> lines)
    {
        var detalle = new XElement(Ns + "DetalleServicio");
        foreach (var l in lines)
        {
            var linea = new XElement(Ns + "LineaDetalle",
                Elem("NumeroLinea", l.NumeroLinea.ToString()),
                new XElement(Ns + "Codigo",
                    Elem("Tipo", "04"),
                    Elem("Codigo", l.Codigo)),
                Elem("Cantidad", l.Cantidad.ToString("F3")),
                Elem("UnidadMedida", l.UnidadMedida),
                Elem("Detalle", l.Detalle),
                Elem("PrecioUnitario", Fmt(l.PrecioUnitario)),
                Elem("MontoTotal", Fmt(l.Cantidad * l.PrecioUnitario)));

            if (l.MontoDescuento > 0)
                linea.Add(new XElement(Ns + "Descuento",
                    Elem("MontoDescuento", Fmt(l.MontoDescuento)),
                    Elem("NaturalezaDescuento", "Descuento comercial")));

            linea.Add(Elem("SubTotal", Fmt(l.SubTotal)));

            if (l.TarifaImpuesto > 0)
                linea.Add(new XElement(Ns + "Impuesto",
                    Elem("Codigo", l.CodigoImpuesto),
                    Elem("CodigoTarifa", l.CodigoTarifaImpuesto),
                    Elem("Tarifa", l.TarifaImpuesto.ToString("F2")),
                    Elem("Monto", Fmt(l.MontoImpuesto))));

            linea.Add(Elem("ImpuestoNeto", Fmt(l.MontoImpuesto)));
            linea.Add(Elem("MontoTotalLinea", Fmt(l.MontoTotalLinea)));

            detalle.Add(linea);
        }
        return detalle;
    }

    private static XElement BuildResumen(FeCrInvoiceData d,
        decimal totalGravado, decimal totalExento, decimal totalVenta,
        decimal totalDescuento, decimal totalImpuesto, decimal totalComprobante) =>
        new(Ns + "ResumenFactura",
            new XElement(Ns + "CodigoTipoMoneda",
                Elem("CodigoMoneda", d.CodigoMoneda),
                Elem("TipoCambio", d.TipoCambio.ToString("F5"))),
            Elem("TotalServGravados", "0.00"),
            Elem("TotalServExentos", "0.00"),
            Elem("TotalServExonerado", "0.00"),
            Elem("TotalMercanciasGravadas", Fmt(totalGravado)),
            Elem("TotalMercanciasExentas", Fmt(totalExento)),
            Elem("TotalMercExonerada", "0.00"),
            Elem("TotalGravado", Fmt(totalGravado)),
            Elem("TotalExento", Fmt(totalExento)),
            Elem("TotalExonerado", "0.00"),
            Elem("TotalVenta", Fmt(totalVenta)),
            Elem("TotalDescuentos", Fmt(totalDescuento)),
            Elem("TotalVentaNeta", Fmt(totalVenta - totalDescuento)),
            Elem("TotalImpuesto", Fmt(totalImpuesto)),
            Elem("TotalIVADevuelto", "0.00"),
            Elem("TotalOtrosCargos", "0.00"),
            Elem("TotalComprobante", Fmt(totalComprobante)));

    private static XElement Elem(string name, string value) =>
        new(Ns + name, value);

    private static string Fmt(decimal v) => v.ToString("F2");
}

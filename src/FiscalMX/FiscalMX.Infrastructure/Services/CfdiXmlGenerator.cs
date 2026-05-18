using FiscalMX.Domain.Services;
using System.Xml.Linq;

namespace FiscalMX.Infrastructure.Services;

/// <summary>
/// Generates a CFDI 4.0 XML document (unsigned) compliant with the SAT XSD.
/// The PAC (stub or real) will add the TimbreFiscalDigital complement and sello.
/// </summary>
public class CfdiXmlGenerator : ICfdiXmlGenerator
{
    private static readonly XNamespace Cfdi = "http://www.sat.gob.mx/cfd/4";
    private static readonly XNamespace Xsi  = "http://www.w3.org/2001/XMLSchema-instance";

    public string Generate(CfdiInvoiceData d)
    {
        var comprobante = new XElement(Cfdi + "Comprobante",
            new XAttribute(XNamespace.Xmlns + "cfdi", Cfdi.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
            new XAttribute(Xsi + "schemaLocation",
                "http://www.sat.gob.mx/cfd/4 http://www.sat.gob.mx/sitio_internet/cfd/4/cfdv40.xsd"),
            new XAttribute("Version", "4.0"),
            d.Serie is not null ? new XAttribute("Serie", d.Serie) : null!,
            d.Folio is not null ? new XAttribute("Folio", d.Folio) : null!,
            new XAttribute("Fecha", d.Fecha.ToString("yyyy-MM-ddTHH:mm:ss")),
            new XAttribute("Sello", ""),
            new XAttribute("FormaPago", d.FormaPago),
            new XAttribute("NoCertificado", ""),
            new XAttribute("Certificado", ""),
            d.Descuento > 0 ? new XAttribute("Descuento", Fmt(d.Descuento)) : null!,
            new XAttribute("SubTotal", Fmt(d.Subtotal)),
            new XAttribute("Moneda", d.Moneda),
            d.Moneda != "MXN" ? new XAttribute("TipoCambio", Fmt(d.TipoCambio)) : null!,
            new XAttribute("Total", Fmt(d.Total)),
            new XAttribute("TipoDeComprobante", d.TipoDeComprobante),
            new XAttribute("Exportacion", d.Exportacion),
            new XAttribute("MetodoPago", d.MetodoPago),
            new XAttribute("LugarExpedicion", d.LugarExpedicion),

            BuildEmisor(d),
            BuildReceptor(d),
            BuildConceptos(d),
            BuildImpuestos(d)
        );

        // Remove null attributes (from conditional null! expressions)
        foreach (var attr in comprobante.Attributes().Where(a => a.Value == "").ToList())
        {
            if (attr.Name.LocalName is not ("Sello" or "NoCertificado" or "Certificado"))
                attr.Remove();
        }

        return comprobante.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildEmisor(CfdiInvoiceData d) =>
        new(Cfdi + "Emisor",
            new XAttribute("Rfc", d.EmisorRfc),
            new XAttribute("Nombre", d.EmisorNombre),
            new XAttribute("RegimenFiscal", d.EmisorRegimenFiscal));

    private static XElement BuildReceptor(CfdiInvoiceData d) =>
        new(Cfdi + "Receptor",
            new XAttribute("Rfc", d.ReceptorRfc),
            new XAttribute("Nombre", d.ReceptorNombre),
            new XAttribute("DomicilioFiscalReceptor", d.ReceptorDomicilioFiscal),
            new XAttribute("RegimenFiscalReceptor", d.ReceptorRegimenFiscal),
            new XAttribute("UsoCFDI", d.ReceptorUsoCfdi));

    private static XElement BuildConceptos(CfdiInvoiceData d) =>
        new(Cfdi + "Conceptos", d.Lines.Select(l =>
        {
            var concepto = new XElement(Cfdi + "Concepto",
                new XAttribute("ClaveProdServ", l.ClaveProdServ),
                new XAttribute("NoIdentificacion", l.NoIdentificacion),
                new XAttribute("Cantidad", Fmt(l.Cantidad)),
                new XAttribute("ClaveUnidad", l.ClaveUnidad),
                new XAttribute("Descripcion", l.Descripcion),
                new XAttribute("ValorUnitario", Fmt(l.ValorUnitario)),
                new XAttribute("Importe", Fmt(l.Importe)),
                l.DescuentoImporte > 0 ? new XAttribute("Descuento", Fmt(l.DescuentoImporte)) : null!,
                new XAttribute("ObjetoImp", l.ObjetoImp));

            if (l.TrasladoImporte > 0 || l.TrasladoTasa == 0)
            {
                concepto.Add(new XElement(Cfdi + "Impuestos",
                    new XElement(Cfdi + "Traslados",
                        new XElement(Cfdi + "Traslado",
                            new XAttribute("Base", Fmt(l.TrasladoBase)),
                            new XAttribute("Impuesto", "002"),
                            new XAttribute("TipoFactor", l.TrasladoTasa == 0 ? "Exento" : "Tasa"),
                            l.TrasladoTasa > 0 ? new XAttribute("TasaOCuota", l.TrasladoTasa.ToString("F6")) : null!,
                            l.TrasladoImporte > 0 ? new XAttribute("Importe", Fmt(l.TrasladoImporte)) : null!))));
            }

            // Remove null attributes
            foreach (var attr in concepto.DescendantsAndSelf()
                .SelectMany(e => e.Attributes())
                .Where(a => a.Value == "")
                .ToList())
                attr.Remove();

            return concepto;
        }));

    private static XElement BuildImpuestos(CfdiInvoiceData d)
    {
        var totalIva = d.Lines.Sum(l => l.TrasladoImporte);
        var baseIva = d.Lines.Sum(l => l.TrasladoBase);
        var tasa = d.Lines.FirstOrDefault()?.TrasladoTasa ?? 0.16m;

        return new XElement(Cfdi + "Impuestos",
            new XAttribute("TotalImpuestosTrasladados", Fmt(totalIva)),
            new XElement(Cfdi + "Traslados",
                new XElement(Cfdi + "Traslado",
                    new XAttribute("Base", Fmt(baseIva)),
                    new XAttribute("Impuesto", "002"),
                    new XAttribute("TipoFactor", "Tasa"),
                    new XAttribute("TasaOCuota", tasa.ToString("F6")),
                    new XAttribute("Importe", Fmt(totalIva)))));
    }

    private static string Fmt(decimal v) => v.ToString("F2");
}

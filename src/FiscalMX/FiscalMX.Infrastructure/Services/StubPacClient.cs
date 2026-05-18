using FiscalMX.Domain.Services;
using System.Xml.Linq;

namespace FiscalMX.Infrastructure.Services;

/// <summary>
/// Development stub: simulates a PAC response by injecting a fake TimbreFiscalDigital complement.
/// Replace with SwSapienPacClient or FinokPacClient when real PAC credentials are available.
/// </summary>
public class StubPacClient : IPacClient
{
    private static readonly XNamespace Tfd = "http://www.sat.gob.mx/TimbreFiscalDigital";
    private static readonly XNamespace Cfdi = "http://www.sat.gob.mx/cfd/4";

    public Task<PacTimbraResponse> TimbrarAsync(PacTimbraRequest request, CancellationToken ct = default)
    {
        try
        {
            var uuid = Guid.NewGuid().ToString().ToUpper();
            var xmlTimbrado = InjectTfd(request.XmlOriginal, uuid);
            return Task.FromResult(new PacTimbraResponse(true, uuid, xmlTimbrado, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PacTimbraResponse(false, null, null, ex.Message));
        }
    }

    public Task<PacCancelResponse> CancelarAsync(PacCancelRequest request, CancellationToken ct = default)
        => Task.FromResult(new PacCancelResponse(true, "<AcuseStub/>", null));

    private static string InjectTfd(string xmlOriginal, string uuid)
    {
        var doc = XDocument.Parse(xmlOriginal);
        var root = doc.Root!;

        var tfdElement = new XElement(Tfd + "TimbreFiscalDigital",
            new XAttribute(XNamespace.Xmlns + "tfd", Tfd.NamespaceName),
            new XAttribute("Version", "1.1"),
            new XAttribute("UUID", uuid),
            new XAttribute("FechaTimbrado", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")),
            new XAttribute("RfcProvCertif", "SAT970701NN3"),
            new XAttribute("SelloCFD", "STUB_SELLO_CFD"),
            new XAttribute("NoCertificadoSAT", "00001000000507802604"),
            new XAttribute("SelloSAT", "STUB_SELLO_SAT"));

        var complemento = root.Element(Cfdi + "Complemento");
        if (complemento is null)
        {
            complemento = new XElement(Cfdi + "Complemento");
            root.Add(complemento);
        }
        complemento.Add(tfdElement);

        // Set the Sello attribute (stub value)
        root.SetAttributeValue("Sello", "STUB_SELLO");

        return doc.ToString(SaveOptions.DisableFormatting);
    }
}

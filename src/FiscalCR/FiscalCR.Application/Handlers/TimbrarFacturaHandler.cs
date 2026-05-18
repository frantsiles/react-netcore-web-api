using FiscalCR.Application.Commands;
using FiscalCR.Application.DTOs;
using FiscalCR.Domain.ElectronicDocuments;
using FiscalCR.Domain.Services;
using Invoicing.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Parties.Domain.Parties;
using Parties.Domain.ValueObjects;

namespace FiscalCR.Application.Handlers;

public class TimbrarFacturaHandler(
    IInvoiceRepository invoiceRepo,
    IElectronicDocumentRepository docRepo,
    IFeCrXmlGenerator xmlGenerator,
    IXmlSigner xmlSigner,
    IHaciendaClient haciendaClient,
    IConfiguration config,
    ILogger<TimbrarFacturaHandler> logger)
    : IRequestHandler<TimbrarFacturaCommand, ElectronicDocumentDto>
{
    public async Task<ElectronicDocumentDto> Handle(TimbrarFacturaCommand request, CancellationToken ct)
    {
        var invoice = await invoiceRepo.GetByIdAsync(request.InvoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice {request.InvoiceId} not found.");

        if (invoice.Status != Invoicing.Domain.Invoices.InvoiceStatus.Issued)
            throw new InvalidOperationException("Only issued invoices can be timbradas.");

        // Idempotent: return existing if already submitted/accepted
        var existing = await docRepo.FindByInvoiceIdAsync(invoice.Id, ct);
        if (existing is not null && existing.Status is ElectronicDocumentStatus.Accepted or ElectronicDocumentStatus.Submitted)
            return ToDto(existing);

        var tenantCfg = LoadTenantConfig(invoice.TenantId);
        var (clave, consecutivo) = ClaveGenerator.Generate(
            tenantCfg.TipoIdentificacion, tenantCfg.NumeroIdentificacion,
            invoice.IssueDate.ToDateTime(TimeOnly.MinValue));

        var doc = existing ?? ElectronicDocument.Create(
            invoice.TenantId, invoice.Id, invoice.InvoiceNumber, clave, consecutivo);

        if (existing is null)
            await docRepo.AddAsync(doc, ct);

        try
        {
            var xmlData = BuildXmlData(invoice, tenantCfg, clave, consecutivo);
            var unsignedXml = xmlGenerator.Generate(xmlData);
            doc.SetUnsignedXml(unsignedXml);

            var signedXml = await xmlSigner.SignAsync(unsignedXml, invoice.TenantId, ct);
            doc.MarkSigned(signedXml);

            var submitReq = new HaciendaSubmitRequest(
                Clave: clave,
                Fecha: invoice.IssueDate.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-ddTHH:mm:sszzz"),
                EmisorTipoId: tenantCfg.TipoIdentificacion,
                EmisorCedula: tenantCfg.NumeroIdentificacion,
                ReceptorTipoId: null,
                ReceptorCedula: null,
                ComprobanteXmlBase64: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(signedXml)));

            var submitted = await haciendaClient.SubmitAsync(submitReq, invoice.TenantId, ct);
            if (submitted) doc.MarkSubmitted();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error timbrating invoice {InvoiceId}", invoice.Id);
            doc.MarkError(ex.Message);
        }

        await docRepo.SaveChangesAsync(ct);
        return ToDto(doc);
    }

    private TenantFeCrConfig LoadTenantConfig(Guid tenantId)
    {
        // In production, load from tenant settings stored in DB.
        // For demo, fall back to appsettings FiscalCR section.
        var section = config.GetSection("FiscalCR");
        return new TenantFeCrConfig(
            RazonSocial: section["EmisorNombre"] ?? "Demo ERP S.A.",
            NombreComercial: section["EmisorNombreComercial"] ?? "Demo ERP",
            TipoIdentificacion: section["EmisorTipoId"] ?? "02",
            NumeroIdentificacion: section["EmisorCedula"] ?? "3101000001",
            CodigoActividad: section["CodigoActividad"] ?? "620200",
            Provincia: section["Provincia"] ?? "1",
            Canton: section["Canton"] ?? "01",
            Distrito: section["Distrito"] ?? "01",
            OtrasSenas: section["OtrasSenas"] ?? "San José, Costa Rica",
            Telefono: section["Telefono"],
            Email: section["Email"] ?? "facturacion@demo.cr",
            HaciendaUsername: section["HaciendaUsername"],
            HaciendaPassword: section["HaciendaPassword"],
            CertificatePath: section["CertificatePath"],
            CertificatePassword: section["CertificatePassword"]);
    }

    private static FeCrInvoiceData BuildXmlData(
        Invoicing.Domain.Invoices.Invoice invoice,
        TenantFeCrConfig cfg,
        string clave, string consecutivo)
    {
        var lines = invoice.Lines.Select((l, i) =>
        {
            var tarifa = invoice.TaxAmount.Amount > 0 ? 13m : 0m;
            var codigoTarifa = tarifa switch { 13m => "08", 4m => "04", 2m => "02", 1m => "01", _ => "07" };
            var taxAmount = l.LineTotal.Amount * tarifa / (100 + tarifa);
            var subTotal = l.LineTotal.Amount - taxAmount;
            return new FeCrLineData(
                NumeroLinea: i + 1,
                Codigo: l.SKU,
                Detalle: l.Description,
                Cantidad: l.Quantity,
                UnidadMedida: "Unid",
                PrecioUnitario: l.UnitPrice.Amount,
                MontoDescuento: l.LineTotal.Amount * l.DiscountPercent / 100,
                SubTotal: subTotal,
                MontoImpuesto: taxAmount,
                TarifaImpuesto: tarifa,
                CodigoImpuesto: tarifa > 0 ? "01" : "",
                CodigoTarifaImpuesto: codigoTarifa,
                MontoTotalLinea: l.LineTotal.Amount);
        }).ToList();

        return new FeCrInvoiceData(
            Clave: clave,
            NumeroConsecutivo: consecutivo,
            FechaEmision: invoice.IssueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local),
            CodigoActividad: cfg.CodigoActividad,
            EmisorNombre: cfg.RazonSocial,
            EmisorTipoId: cfg.TipoIdentificacion,
            EmisorCedula: cfg.NumeroIdentificacion,
            EmisorNombreComercial: cfg.NombreComercial,
            EmisorProvincia: cfg.Provincia,
            EmisorCanton: cfg.Canton,
            EmisorDistrito: cfg.Distrito,
            EmisorOtrasSenas: cfg.OtrasSenas,
            EmisorTelefono: cfg.Telefono,
            EmisorEmail: cfg.Email,
            ReceptorNombre: null,
            ReceptorTipoId: null,
            ReceptorCedula: null,
            ReceptorEmail: null,
            CondicionVenta: "01",
            MedioPago: "01",
            PlazoCredito: null,
            Lines: lines,
            CodigoMoneda: invoice.CurrencyCode,
            TipoCambio: 1m);
    }

    private static ElectronicDocumentDto ToDto(ElectronicDocument doc) => new(
        doc.Id, doc.InvoiceId, doc.InvoiceNumber, doc.Clave, doc.NumeroConsecutivo,
        doc.Status.ToString(), doc.HaciendaEstado, doc.HaciendaMensaje,
        doc.CreatedAt, doc.SubmittedAt, doc.AcknowledgedAt);
}

/// <summary>Generates the 50-character CR FE clave and 20-character numero consecutivo.</summary>
internal static class ClaveGenerator
{
    // Clave format (50 chars):
    // 3: country (506)
    // 8: date DDMMAAAA
    // 1: tipo id emisor
    // 12: cedula emisor (padded)
    // 20: numero consecutivo (TipoDoc 2 + Sucursal 3 + Terminal 5 + Secuencia 10)
    // 1: situacion (1=normal)
    // 5: random security code
    private static long _sequence = 0;

    public static (string Clave, string Consecutivo) Generate(
        string tipoId, string cedula, DateTime fecha)
    {
        var seq = Interlocked.Increment(ref _sequence);
        var consecutivo = BuildConsecutivo(seq);
        var cedulaPadded = cedula.PadLeft(12, '0');
        var date = fecha.ToString("ddMMyyyy");
        var security = Random.Shared.Next(10000, 99999).ToString();
        var clave = $"506{date}{tipoId}{cedulaPadded}{consecutivo}1{security}";
        return (clave, consecutivo);
    }

    // NumeroConsecutivo = TipoDoc(2) + Sucursal(3) + Terminal(5) + Secuencia(10) = 20 chars
    private static string BuildConsecutivo(long seq) =>
        $"01" + "001" + "00001" + seq.ToString().PadLeft(10, '0');
}

using FiscalMX.Application.DTOs;
using FiscalMX.Domain.CfdiDocuments;
using FiscalMX.Domain.Services;
using Invoicing.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Parties.Infrastructure.Persistence;

namespace FiscalMX.Application.Commands;

public record TimbrarCfdiCommand(Guid InvoiceId) : IRequest<CfdiDocumentDto>;

public class TimbrarCfdiHandler(
    ICfdiDocumentRepository repo,
    ICfdiXmlGenerator xmlGenerator,
    IPacClient pacClient,
    InvoicingDbContext invoicingDb,
    PartiesDbContext partiesDb,
    IConfiguration config,
    ILogger<TimbrarCfdiHandler> logger)
    : IRequestHandler<TimbrarCfdiCommand, CfdiDocumentDto>
{
    public async Task<CfdiDocumentDto> Handle(TimbrarCfdiCommand req, CancellationToken ct)
    {
        var invoice = await invoicingDb.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == req.InvoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice {req.InvoiceId} not found.");

        if (invoice.Status != Invoicing.Domain.Invoices.InvoiceStatus.Issued)
            throw new InvalidOperationException("Solo se pueden timbrar facturas emitidas.");

        // Idempotencia
        var existing = await repo.FindByInvoiceIdAsync(invoice.Id, ct);
        if (existing is { Status: CfdiStatus.Timbrado })
            return ToDto(existing);

        var tenantCfg = LoadTenantConfig(invoice.TenantId);

        // Datos del receptor desde Parties
        var customer = await partiesDb.Parties
            .FirstOrDefaultAsync(p => p.Id == invoice.CustomerId, ct);

        var serie = "A";
        var folio = invoice.InvoiceNumber.Replace("FAC-", "").Replace("-", "");

        var doc = existing ?? CfdiDocument.Create(
            invoice.TenantId, invoice.Id, invoice.InvoiceNumber, serie, folio);

        if (existing is null)
            await repo.AddAsync(doc, ct);

        try
        {
            var xmlData = BuildCfdiData(invoice, customer, tenantCfg, serie, folio);
            var unsignedXml = xmlGenerator.Generate(xmlData);
            doc.SetOriginalXml(unsignedXml);

            var timbraResp = await pacClient.TimbrarAsync(
                new PacTimbraRequest(unsignedXml, invoice.TenantId), ct);

            if (timbraResp.Success && timbraResp.Uuid is not null)
                doc.MarkTimbrado(timbraResp.Uuid, timbraResp.XmlTimbrado ?? unsignedXml);
            else
                doc.MarkError(timbraResp.ErrorMessage ?? "Error desconocido del PAC");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error timbrando CFDI para factura {InvoiceId}", invoice.Id);
            doc.MarkError(ex.Message);
        }

        await repo.SaveChangesAsync(ct);
        return ToDto(doc);
    }

    private TenantFeMxConfig LoadTenantConfig(Guid tenantId)
    {
        var s = config.GetSection("FiscalMX");
        return new TenantFeMxConfig(
            EmisorRfc: s["EmisorRfc"] ?? "AAA010101AAA",
            EmisorNombre: s["EmisorNombre"] ?? "EMPRESA DEMO S.A. DE C.V.",
            RegimenFiscal: s["RegimenFiscal"] ?? "601",
            LugarExpedicion: s["LugarExpedicion"] ?? "64000",
            PacApiUrl: s["PacApiUrl"],
            PacToken: s["PacToken"]);
    }

    private static CfdiInvoiceData BuildCfdiData(
        Invoicing.Domain.Invoices.Invoice invoice,
        Parties.Domain.Parties.Party? customer,
        TenantFeMxConfig cfg,
        string serie, string folio)
    {
        var receptorRfc = customer?.TaxId?.Value ?? "XAXX010101000"; // Público en general
        var receptorNombre = customer?.LegalName ?? customer?.TradeName ?? "PUBLICO EN GENERAL";

        var lines = invoice.Lines.Select(l =>
        {
            var base_ = l.Quantity * l.UnitPrice.Amount * (1 - l.DiscountPercent / 100m);
            var tasa = 0.16m;
            var iva = Math.Round(base_ * tasa, 2);
            return new CfdiLineData(
                ClaveProdServ: "01010101",
                ClaveUnidad: "H87",
                NoIdentificacion: string.IsNullOrEmpty(l.SKU)
                    ? (l.CatalogItemId?.ToString("N")[..8] ?? "00000000")
                    : l.SKU,
                Descripcion: l.Description,
                Cantidad: l.Quantity,
                ValorUnitario: Math.Round(l.UnitPrice.Amount, 6),
                Importe: Math.Round(base_, 2),
                DescuentoImporte: Math.Round(base_ * l.DiscountPercent / 100m, 2),
                TrasladoBase: Math.Round(base_, 2),
                TrasladoTasa: tasa,
                TrasladoImporte: iva,
                ObjetoImp: "02");
        }).ToList().AsReadOnly();

        return new CfdiInvoiceData(
            EmisorRfc: cfg.EmisorRfc,
            EmisorNombre: cfg.EmisorNombre.ToUpperInvariant(),
            EmisorRegimenFiscal: cfg.RegimenFiscal,
            LugarExpedicion: cfg.LugarExpedicion,
            ReceptorRfc: receptorRfc,
            ReceptorNombre: receptorNombre.ToUpperInvariant(),
            ReceptorDomicilioFiscal: cfg.LugarExpedicion,
            ReceptorRegimenFiscal: "616",
            ReceptorUsoCfdi: "G03",
            Serie: serie,
            Folio: folio,
            Fecha: invoice.IssueDate.ToDateTime(TimeOnly.MinValue),
            FormaPago: "03",
            MetodoPago: "PUE",
            Moneda: invoice.CurrencyCode == "MXN" ? "MXN" : "USD",
            TipoCambio: 1m,
            TipoDeComprobante: "I",
            Exportacion: "01",
            Subtotal: invoice.Subtotal.Amount,
            Descuento: invoice.Lines.Sum(l => Math.Round(l.Quantity * l.UnitPrice.Amount * l.DiscountPercent / 100m, 2)),
            TotalImpuestos: invoice.TaxAmount.Amount,
            Total: invoice.TotalAmount.Amount,
            Lines: lines);
    }

    internal static CfdiDocumentDto ToDto(CfdiDocument d) => new(
        d.Id, d.InvoiceId, d.InvoiceNumber,
        d.Uuid, d.Serie, d.Folio, d.Status.ToString(),
        d.PacMensaje, d.CancelMotivo,
        d.CreatedAt, d.TimbradoAt, d.CancelledAt);
}

using Invoicing.Application.DTOs;
using Parties.Application.Common.Dtos;
using Purchasing.Application.Common.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sales.Application.Common.Dtos;

namespace Api.WebApi.Infrastructure.Pdf;

public static class PdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Quote ─────────────────────────────────────────────────────────────────

    public static byte[] GenerateQuotePdf(QuoteDto quote, PartyDto? customer)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSettings(page);

                page.Header().Element(c => BuildHeader(c, "COTIZACIÓN", quote.QuoteNumber,
                    customer?.TradeName ?? customer?.LegalName ?? quote.CustomerId.ToString("N")[..8] + "…",
                    $"Válida hasta: {quote.ValidUntil:dd/MM/yyyy}",
                    $"Fecha: {quote.CreatedAt:dd/MM/yyyy}",
                    quote.CountryCode));

                page.Content().Element(c => BuildLineItemsTable(c,
                    quote.Lines.Select(l => new LineItem(l.SKU, l.ItemName, l.Quantity,
                        l.UnitPrice, l.DiscountPercent, l.LineTotal, l.CurrencyCode)).ToList(),
                    quote.CurrencyCode, quote.Subtotal, quote.TaxAmount, quote.Total));

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Sales Order ───────────────────────────────────────────────────────────

    public static byte[] GenerateSalesOrderPdf(SalesOrderDto order, PartyDto? customer)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSettings(page);

                page.Header().Element(c => BuildHeader(c, "ORDEN DE VENTA", order.OrderNumber,
                    customer?.TradeName ?? customer?.LegalName ?? order.CustomerId.ToString("N")[..8] + "…",
                    $"Fecha pedido: {order.OrderDate:dd/MM/yyyy}",
                    order.RequestedDeliveryDate.HasValue
                        ? $"Entrega: {order.RequestedDeliveryDate.Value:dd/MM/yyyy}"
                        : "Sin fecha de entrega",
                    order.CountryCode));

                page.Content().Element(c => BuildLineItemsTable(c,
                    order.Lines.Select(l => new LineItem(l.SKU, l.ItemName, l.Quantity,
                        l.UnitPrice, l.DiscountPercent, l.LineTotal, l.CurrencyCode)).ToList(),
                    order.CurrencyCode, order.Subtotal, order.TaxAmount, order.Total));

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Invoice ───────────────────────────────────────────────────────────────

    public static byte[] GenerateInvoicePdf(InvoiceDto invoice, PartyDto? customer)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSettings(page);

                page.Header().Element(c => BuildHeader(c, "FACTURA", invoice.InvoiceNumber,
                    customer?.TradeName ?? customer?.LegalName ?? invoice.CustomerId.ToString("N")[..8] + "…",
                    $"Emisión: {invoice.IssueDate:dd/MM/yyyy}",
                    $"Vencimiento: {invoice.DueDate:dd/MM/yyyy}",
                    invoice.CurrencyCode));

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Item().Element(c => BuildLineItemsTable(c,
                        invoice.Lines.Select(l => new LineItem(l.SKU, l.Description, l.Quantity,
                            l.UnitPrice, l.DiscountPercent, l.LineTotal, l.CurrencyCode)).ToList(),
                        invoice.CurrencyCode, invoice.Subtotal, invoice.TaxAmount, invoice.TotalAmount));

                    if (invoice.PaidAmount > 0)
                    {
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(260).Border(1).BorderColor(Colors.Grey.Lighten2)
                                .Padding(8).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Pagado").FontSize(10).FontColor(Colors.Green.Darken2);
                                        r.ConstantItem(100).AlignRight()
                                            .Text(Fmt(invoice.PaidAmount, invoice.CurrencyCode)).FontSize(10).FontColor(Colors.Green.Darken2);
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Saldo pendiente").FontSize(11).Bold();
                                        r.ConstantItem(100).AlignRight()
                                            .Text(Fmt(invoice.BalanceDue, invoice.CurrencyCode)).FontSize(11).Bold();
                                    });
                                });
                        });
                    }
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Purchase Order ────────────────────────────────────────────────────────

    public static byte[] GeneratePurchaseOrderPdf(PurchaseOrderDto po, PartyDto? supplier)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSettings(page);

                page.Header().Element(c => BuildHeader(c, "ORDEN DE COMPRA", po.PoNumber,
                    supplier?.TradeName ?? supplier?.LegalName ?? po.SupplierId.ToString("N")[..8] + "…",
                    $"Fecha: {po.CreatedAt:dd/MM/yyyy}",
                    po.ExpectedDeliveryDate.HasValue
                        ? $"Entrega esperada: {po.ExpectedDeliveryDate.Value:dd/MM/yyyy}"
                        : "Sin fecha de entrega",
                    po.CountryCode));

                page.Content().Element(c => BuildLineItemsTable(c,
                    po.Lines.Select(l => new LineItem(l.Sku, l.ItemName, l.QuantityOrdered,
                        l.UnitCostAmount, 0, l.LineTotal, l.CurrencyCode)).ToList(),
                    po.CurrencyCode, po.Subtotal, 0m, po.Total));

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Shared layout helpers ─────────────────────────────────────────────────

    private sealed record LineItem(
        string SKU, string Name, decimal Qty, decimal UnitPrice,
        decimal DiscountPct, decimal LineTotal, string Currency);

    private static void ApplyPageSettings(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(36);
        page.DefaultTextStyle(s => s.FontSize(10));
    }

    private static void BuildHeader(
        IContainer c, string docType, string docNumber,
        string partyName, string date1, string date2, string country)
    {
        c.Column(col =>
        {
            // Top band
            col.Item().Background("#1e3a5f").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text("ERP Platform").FontSize(16).Bold()
                        .FontColor(Colors.White);
                    inner.Item().Text($"{docType}  ·  {docNumber}").FontSize(13)
                        .FontColor("#a8c8e8");
                });
                row.ConstantItem(180).Column(inner =>
                {
                    inner.Item().AlignRight().Text(date1).FontSize(9).FontColor(Colors.White);
                    inner.Item().AlignRight().Text(date2).FontSize(9).FontColor("#a8c8e8");
                    inner.Item().AlignRight().Text($"País: {country}").FontSize(9).FontColor("#a8c8e8");
                });
            });

            // Party row
            col.Item().Background("#f0f4f8").Padding(8).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text("CLIENTE / PROVEEDOR").FontSize(8).FontColor(Colors.Grey.Darken2);
                    inner.Item().Text(partyName).FontSize(11).Bold();
                });
            });

            col.Item().PaddingBottom(4);
        });
    }

    private static void BuildLineItemsTable(IContainer c,
        List<LineItem> lines, string currency,
        decimal subtotal, decimal tax, decimal total)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(60);   // SKU
                cols.RelativeColumn(3);    // Description
                cols.ConstantColumn(48);   // Qty
                cols.ConstantColumn(80);   // Unit price
                cols.ConstantColumn(48);   // Disc%
                cols.ConstantColumn(90);   // Total
            });

            // Header row
            static void HeaderCell(IContainer hc, string text) =>
                hc.Background("#1e3a5f").Padding(5)
                  .Text(text).FontSize(9).Bold().FontColor(Colors.White);

            table.Header(h =>
            {
                h.Cell().Element(hc => HeaderCell(hc, "SKU"));
                h.Cell().Element(hc => HeaderCell(hc, "Descripción"));
                h.Cell().Element(hc => HeaderCell(hc, "Cant."));
                h.Cell().Element(hc => HeaderCell(hc, "P. Unitario"));
                h.Cell().Element(hc => HeaderCell(hc, "Desc.%"));
                h.Cell().Element(hc => HeaderCell(hc, "Total"));
            });

            // Data rows
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                void Cell(IContainer dc, string text, bool rightAlign = false)
                {
                    var t = dc.Background(bg).Padding(5).Text(text).FontSize(9);
                    if (rightAlign) t.AlignRight();
                }

                table.Cell().Element(dc => Cell(dc, line.SKU));
                table.Cell().Element(dc => Cell(dc, line.Name));
                table.Cell().Element(dc => Cell(dc, line.Qty.ToString("N2"), true));
                table.Cell().Element(dc => Cell(dc, Fmt(line.UnitPrice, line.Currency), true));
                table.Cell().Element(dc => Cell(dc, line.DiscountPct > 0 ? $"{line.DiscountPct:N1}%" : "—", true));
                table.Cell().Element(dc => Cell(dc, Fmt(line.LineTotal, line.Currency), true));
            }

            // Totals section
            var colSpan = 5u;
            if (subtotal != total)
            {
                table.Cell().ColumnSpan(colSpan).BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                    .Background("#f7f9fc").Padding(5)
                    .Text("Subtotal").FontSize(9).AlignRight();
                table.Cell().BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                    .Background("#f7f9fc").Padding(5)
                    .Text(Fmt(subtotal, currency)).FontSize(9).AlignRight();

                if (tax > 0)
                {
                    table.Cell().ColumnSpan(colSpan).Background("#f7f9fc").Padding(5)
                        .Text("Impuestos").FontSize(9).AlignRight();
                    table.Cell().Background("#f7f9fc").Padding(5)
                        .Text(Fmt(tax, currency)).FontSize(9).AlignRight();
                }
            }

            table.Cell().ColumnSpan(colSpan).Background("#1e3a5f").Padding(6)
                .Text("TOTAL").FontSize(10).Bold().FontColor(Colors.White).AlignRight();
            table.Cell().Background("#1e3a5f").Padding(6)
                .Text(Fmt(total, currency)).FontSize(10).Bold().FontColor(Colors.White).AlignRight();
        });
    }

    private static void BuildFooter(IContainer c)
    {
        c.Row(row =>
        {
            row.RelativeItem().Text($"Generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(100).AlignRight()
                .Text(text => { text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium); text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium); text.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium); text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium); });
        });
    }

    private static string Fmt(decimal amount, string currency) =>
        $"{currency} {amount:N2}";
}

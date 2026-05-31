using Invoicing.Application.DTOs;
using Parties.Application.Common.Dtos;
using Payroll.Application.DTOs;
using Purchasing.Application.Common.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reporting.Application.Common.Dtos;
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

    // ── Paystub ───────────────────────────────────────────────────────────────

    public static byte[] GeneratePaystubPdf(PayrollRunDto run, PayrollEntryDto entry, string companyName)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(s => s.FontSize(10));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().Background("#1e3a5f").Padding(14).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text(companyName).FontSize(16).Bold().FontColor(Colors.White);
                            inner.Item().Text("RECIBO DE PAGO").FontSize(11).FontColor("#a8c8e8");
                        });
                        row.ConstantItem(200).Column(inner =>
                        {
                            inner.Item().AlignRight().Text(run.RunNumber).FontSize(9).FontColor(Colors.White);
                            inner.Item().AlignRight()
                                .Text($"Período: {run.PeriodStart} → {run.PeriodEnd}")
                                .FontSize(9).FontColor("#a8c8e8");
                            inner.Item().AlignRight()
                                .Text(run.PeriodType == "Monthly" ? "Mensual" : "Quincenal")
                                .FontSize(9).FontColor("#a8c8e8");
                        });
                    });

                    col.Item().Background("#f0f4f8").Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text("EMPLEADO").FontSize(8).FontColor(Colors.Grey.Darken2);
                            inner.Item().Text(entry.EmployeeName).FontSize(12).Bold();
                            inner.Item().Text($"N° {entry.EmployeeNumber}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(160).Column(inner =>
                        {
                            inner.Item().AlignRight().Text("Moneda").FontSize(8).FontColor(Colors.Grey.Darken2);
                            inner.Item().AlignRight().Text(run.CurrencyCode).FontSize(11).Bold();
                        });
                    });

                    col.Item().PaddingBottom(6);
                });

                // Content
                page.Content().Column(col =>
                {
                    // ── Ingresos ──────────────────────────────────────────────
                    col.Item().Text("INGRESOS").FontSize(9).Bold().FontColor("#1e3a5f");
                    col.Item().PaddingBottom(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); });
                        void Row2(string label, decimal value, bool bold = false)
                        {
                            var bg = Colors.White;
                            var el = t.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6)
                                .Text(label).FontSize(9);
                            if (bold) el.Bold();
                            var er = t.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(6)
                                .AlignRight().Text(Fmt(value, run.CurrencyCode)).FontSize(9);
                            if (bold) er.Bold();
                        }
                        Row2("Salario base", entry.BaseSalary);
                        if (entry.OvertimePay > 0) Row2("Horas extra", entry.OvertimePay);
                        t.Cell().ColumnSpan(2).BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                            .Background("#f7f9fc").PaddingVertical(5).PaddingHorizontal(6)
                            .Text("Salario bruto").FontSize(9).Bold();
                        t.Cell().Background("#f7f9fc").PaddingVertical(5).PaddingHorizontal(6)
                            .AlignRight().Text(Fmt(entry.TotalGross, run.CurrencyCode)).FontSize(9).Bold();
                    });

                    col.Item().PaddingTop(10).Text("DEDUCCIONES DEL EMPLEADO").FontSize(9).Bold().FontColor("#1e3a5f");
                    col.Item().PaddingBottom(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); });
                        void DeductRow(string label, string detail, decimal value)
                        {
                            t.Cell().Background(Colors.White).PaddingVertical(4).PaddingHorizontal(6)
                                .Column(c2 =>
                                {
                                    c2.Item().Text(label).FontSize(9);
                                    c2.Item().Text(detail).FontSize(8).FontColor(Colors.Grey.Medium);
                                });
                            t.Cell().Background(Colors.White).PaddingVertical(4).PaddingHorizontal(6)
                                .AlignRight().Text($"- {Fmt(value, run.CurrencyCode)}").FontSize(9).FontColor(Colors.Red.Darken2);
                        }
                        DeductRow("CCSS (empleado)", "9.17% SEM+IVM+otras", entry.CcssEmployee);
                        DeductRow("Banco Popular", "1.00%", entry.BancoPopular);
                        if (entry.IncomeTax > 0)
                            DeductRow("Impuesto sobre la renta", "Art. 33 LISR", entry.IncomeTax);
                        t.Cell().ColumnSpan(2).BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                            .Background("#fff5f5").PaddingVertical(5).PaddingHorizontal(6)
                            .Text("Total deducciones").FontSize(9).Bold().FontColor(Colors.Red.Darken2);
                        t.Cell().Background("#fff5f5").PaddingVertical(5).PaddingHorizontal(6)
                            .AlignRight().Text($"- {Fmt(entry.TotalDeductions, run.CurrencyCode)}").FontSize(9).Bold().FontColor(Colors.Red.Darken2);
                    });

                    // ── Salario Neto ──────────────────────────────────────────
                    col.Item().PaddingTop(10)
                        .Background("#1e3a5f").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text("SALARIO NETO A PAGAR").FontSize(12).Bold().FontColor(Colors.White);
                            row.ConstantItem(180).AlignRight()
                                .Text(Fmt(entry.NetPay, run.CurrencyCode)).FontSize(14).Bold().FontColor("#a8c8e8");
                        });

                    // ── Cargas Patronales (informativo) ───────────────────────
                    col.Item().PaddingTop(12).Text("CARGAS PATRONALES (informativo)").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingBottom(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); });
                        void PatRow(string label, string detail, decimal value)
                        {
                            t.Cell().Background(Colors.Grey.Lighten5).PaddingVertical(4).PaddingHorizontal(6)
                                .Column(c2 =>
                                {
                                    c2.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken2);
                                    c2.Item().Text(detail).FontSize(8).FontColor(Colors.Grey.Medium);
                                });
                            t.Cell().Background(Colors.Grey.Lighten5).PaddingVertical(4).PaddingHorizontal(6)
                                .AlignRight().Text(Fmt(value, run.CurrencyCode)).FontSize(9).FontColor(Colors.Grey.Darken2);
                        }
                        PatRow("CCSS patronal", "26.67%", entry.CcssEmployer);
                        PatRow("INS patronal", "1.00%", entry.InsEmployer);
                        PatRow("Fondo capitalización laboral", "3.00%", entry.Fcl);
                        t.Cell().ColumnSpan(2).BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(6)
                            .Text("Costo total para la empresa").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                        t.Cell().Background(Colors.Grey.Lighten4).PaddingVertical(5).PaddingHorizontal(6)
                            .AlignRight().Text(Fmt(entry.TotalLaborCost, run.CurrencyCode)).FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                    });
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Profit & Loss ─────────────────────────────────────────────────────────

    public static byte[] GeneratePnlPdf(ProfitAndLossReportDto pnl)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSettings(page);

                page.Header().Element(c => BuildReportHeader(c,
                    "ESTADO DE RESULTADOS",
                    $"Del {pnl.From:dd/MM/yyyy} al {pnl.To:dd/MM/yyyy}"));

                page.Content().PaddingTop(8).Column(col =>
                {
                    foreach (var section in pnl.Sections)
                    {
                        col.Item().PaddingTop(10).Text(section.Section.ToUpperInvariant())
                            .FontSize(9).Bold().FontColor("#1e3a5f");

                        col.Item().PaddingBottom(4).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); });
                            foreach (var line in section.Lines)
                            {
                                t.Cell().Background(Colors.White).Padding(4)
                                    .Text($"  {line.AccountNumber}  {line.AccountName}").FontSize(9);
                                t.Cell().Background(Colors.White).Padding(4).AlignRight()
                                    .Text(Fmt(line.Amount, "")).FontSize(9);
                            }
                            t.Cell().Background("#f0f4f8").Padding(5)
                                .Text($"Total {section.Section}").FontSize(9).Bold();
                            t.Cell().Background("#f0f4f8").Padding(5).AlignRight()
                                .Text(Fmt(section.Total, "")).FontSize(9).Bold();
                        });
                    }

                    col.Item().PaddingTop(14).Background("#1e3a5f").Padding(10).Column(inner =>
                    {
                        void SummaryRow(string label, decimal value) =>
                            inner.Item().Row(r =>
                            {
                                r.RelativeItem().Text(label).FontSize(10).FontColor(Colors.White);
                                r.ConstantItem(140).AlignRight()
                                    .Text(Fmt(value, "")).FontSize(10).Bold().FontColor("#a8c8e8");
                            });

                        SummaryRow("Utilidad bruta", pnl.GrossProfit);
                        SummaryRow("Resultado operativo", pnl.OperatingIncome);
                        inner.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("UTILIDAD NETA").FontSize(12).Bold().FontColor(Colors.White);
                            r.ConstantItem(140).AlignRight()
                                .Text(Fmt(pnl.NetIncome, "")).FontSize(13).Bold()
                                .FontColor(pnl.NetIncome >= 0 ? "#6ee7b7" : "#fca5a5");
                        });
                    });
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Balance Sheet ─────────────────────────────────────────────────────────

    public static byte[] GenerateBalanceSheetPdf(BalanceSheetReportDto bs)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSettings(page);

                page.Header().Element(c => BuildReportHeader(c,
                    "BALANCE GENERAL",
                    $"Al {bs.AsOf:dd/MM/yyyy}"));

                page.Content().PaddingTop(8).Column(col =>
                {
                    void Section(BalanceSheetSectionDto s, string color)
                    {
                        col.Item().PaddingTop(10).Text(s.Section.ToUpperInvariant())
                            .FontSize(9).Bold().FontColor("#1e3a5f");
                        col.Item().PaddingBottom(4).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(); });
                            foreach (var line in s.Lines)
                            {
                                t.Cell().Background(Colors.White).Padding(4)
                                    .Text($"  {line.AccountNumber}  {line.AccountName}").FontSize(9);
                                t.Cell().Background(Colors.White).Padding(4).AlignRight()
                                    .Text(Fmt(line.Amount, "")).FontSize(9);
                            }
                            t.Cell().Background(color).Padding(5)
                                .Text($"Total {s.Section}").FontSize(9).Bold().FontColor(Colors.White);
                            t.Cell().Background(color).Padding(5).AlignRight()
                                .Text(Fmt(s.Total, "")).FontSize(9).Bold().FontColor(Colors.White);
                        });
                    }

                    Section(bs.Assets, "#1e3a5f");
                    Section(bs.Liabilities, "#374151");
                    Section(bs.Equity, "#374151");

                    var balanced = bs.IsBalanced ? "✓ CUADRADO" : "✗ DESCUADRADO";
                    var balColor = bs.IsBalanced ? "#065f46" : "#991b1b";
                    col.Item().PaddingTop(12).Background(balColor).Padding(8).AlignCenter()
                        .Text(balanced).FontSize(10).Bold().FontColor(Colors.White);
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Trial Balance ─────────────────────────────────────────────────────────

    public static byte[] GenerateTrialBalancePdf(TrialBalanceReportDto tb)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ApplyPageSettings(page);

                page.Header().Element(c => BuildReportHeader(c,
                    "BALANCE DE COMPROBACIÓN",
                    $"Período fiscal: {tb.FiscalPeriod}"));

                page.Content().PaddingTop(8).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(60);   // No. cuenta
                        c.RelativeColumn(3);    // Nombre
                        c.RelativeColumn();     // Tipo
                        c.ConstantColumn(90);   // Débito
                        c.ConstantColumn(90);   // Crédito
                    });

                    static void HCell(IContainer hc, string text) =>
                        hc.Background("#1e3a5f").Padding(5)
                          .Text(text).FontSize(9).Bold().FontColor(Colors.White);

                    t.Header(h =>
                    {
                        h.Cell().Element(hc => HCell(hc, "Cuenta"));
                        h.Cell().Element(hc => HCell(hc, "Nombre"));
                        h.Cell().Element(hc => HCell(hc, "Tipo"));
                        h.Cell().Element(hc => HCell(hc, "Débito"));
                        h.Cell().Element(hc => HCell(hc, "Crédito"));
                    });

                    for (var i = 0; i < tb.Lines.Count; i++)
                    {
                        var line = tb.Lines[i];
                        var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                        void DCell(IContainer dc, string text, bool right = false)
                        {
                            var tx = dc.Background(bg).Padding(4).Text(text).FontSize(9);
                            if (right) tx.AlignRight();
                        }
                        t.Cell().Element(dc => DCell(dc, line.AccountNumber));
                        t.Cell().Element(dc => DCell(dc, line.AccountName));
                        t.Cell().Element(dc => DCell(dc, line.AccountType));
                        t.Cell().Element(dc => DCell(dc, Fmt(line.DebitBalance, ""), true));
                        t.Cell().Element(dc => DCell(dc, Fmt(line.CreditBalance, ""), true));
                    }

                    // Totals
                    t.Cell().ColumnSpan(3).Background("#1e3a5f").Padding(5)
                        .Text("TOTALES").FontSize(9).Bold().FontColor(Colors.White).AlignRight();
                    t.Cell().Background("#1e3a5f").Padding(5).AlignRight()
                        .Text(Fmt(tb.TotalDebits, "")).FontSize(9).Bold().FontColor(Colors.White);
                    t.Cell().Background("#1e3a5f").Padding(5).AlignRight()
                        .Text(Fmt(tb.TotalCredits, "")).FontSize(9).Bold().FontColor(Colors.White);
                });

                page.Footer().Element(BuildFooter);
            });
        }).GeneratePdf();
    }

    // ── Shared layout helpers ─────────────────────────────────────────────────

    private sealed record LineItem(
        string SKU, string Name, decimal Qty, decimal UnitPrice,
        decimal DiscountPct, decimal LineTotal, string Currency);

    private static void BuildReportHeader(IContainer c, string title, string subtitle)
    {
        c.Column(col =>
        {
            col.Item().Background("#1e3a5f").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(inner =>
                {
                    inner.Item().Text("ERP Platform").FontSize(14).Bold().FontColor(Colors.White);
                    inner.Item().Text(title).FontSize(16).Bold().FontColor("#a8c8e8");
                });
                row.ConstantItem(200).AlignRight()
                    .Text(subtitle).FontSize(10).FontColor(Colors.White);
            });
            col.Item().PaddingBottom(4);
        });
    }

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

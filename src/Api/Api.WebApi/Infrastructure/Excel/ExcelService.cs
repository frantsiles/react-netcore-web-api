using ClosedXML.Excel;
using Reporting.Application.Common.Dtos;

namespace Api.WebApi.Infrastructure.Excel;

public static class ExcelService
{
    public static byte[] GenerateInventoryExcel(InventoryPositionReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Inventario");

        var headers = new[] { "SKU", "Artículo", "Almacén", "Cantidad", "Punto de reorden", "Bajo stock" };
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (var r = 0; r < report.Lines.Count; r++)
        {
            var line = report.Lines[r];
            var row = r + 2;
            ws.Cell(row, 1).Value = line.Sku;
            ws.Cell(row, 2).Value = line.ItemName;
            ws.Cell(row, 3).Value = line.WarehouseName;
            ws.Cell(row, 4).Value = line.QuantityOnHand;
            ws.Cell(row, 5).Value = line.ReorderPoint;
            ws.Cell(row, 6).Value = line.BelowReorderPoint ? "Sí" : "No";
            if (line.BelowReorderPoint)
                ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
        }

        ws.Columns().AdjustToContents();
        ws.Cell(report.Lines.Count + 2, 1).Value = $"Total artículos: {report.TotalItems} | Total unidades: {report.TotalUnits}";
        ws.Cell(report.Lines.Count + 2, 1).Style.Font.Italic = true;

        return ToBytes(wb);
    }

    public static byte[] GenerateAgingExcel(AgingReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(report.Type == "AR" ? "CxC" : "CxP");

        ws.Cell(1, 1).Value = report.Type == "AR" ? "Cuentas por Cobrar" : "Cuentas por Pagar";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Al: {report.AsOf:dd/MM/yyyy}";
        ws.Cell(2, 1).Style.Font.Italic = true;

        var row = 4;
        foreach (var bucket in report.Buckets)
        {
            var headerCell = ws.Cell(row, 1);
            headerCell.Value = $"Vencimiento: {bucket.Label} días";
            headerCell.Style.Font.Bold = true;
            headerCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            headerCell.Style.Font.FontColor = XLColor.White;
            ws.Range(row, 1, row, 6).Merge();
            row++;

            var cols = new[] { "Referencia", "Parte", "Fecha doc.", "Vencimiento", "Monto", "Días vencido" };
            for (var c = 0; c < cols.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = cols[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#e8edf5");
            }
            row++;

            foreach (var line in bucket.Lines)
            {
                ws.Cell(row, 1).Value = line.DocumentReference;
                ws.Cell(row, 2).Value = line.Party;
                ws.Cell(row, 3).Value = line.DocumentDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 4).Value = line.DueDate.ToString("dd/MM/yyyy");
                ws.Cell(row, 5).Value = line.Amount;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Value = line.DaysOverdue;
                row++;
            }

            ws.Cell(row, 4).Value = "Total";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Cell(row, 5).Value = bucket.BucketTotal;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Style.Font.Bold = true;
            row += 2;
        }

        ws.Cell(row, 4).Value = "GRAN TOTAL";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = report.GrandTotal;
        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 5).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}

using Api.WebApi.Infrastructure.Pdf;
using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Application.Commands;
using Payroll.Application.Queries;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController(IMediator mediator) : ControllerBase
{
    [HttpPost("runs")]
    public async Task<IActionResult> Create([FromBody] CreatePayrollRunCommand cmd, CancellationToken ct)
        => Ok(await mediator.Send(cmd, ct));

    [HttpGet("runs/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPayrollRunQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("runs")]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new ListPayrollRunsQuery(skip, take), ct));

    [HttpPost("runs/{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ConfirmPayrollRunCommand(id), ct));

    [HttpPost("runs/{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new MarkPayrollRunPaidCommand(id), ct));

    [HttpGet("runs/{runId:guid}/entries/{entryId:guid}/paystub")]
    public async Task<IActionResult> Paystub(Guid runId, Guid entryId, CancellationToken ct)
    {
        var run = await mediator.Send(new GetPayrollRunQuery(runId), ct);
        if (run is null) return NotFound();
        var entry = run.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null) return NotFound();
        var pdf = PdfService.GeneratePaystubPdf(run, entry, "ERP Platform");
        return File(pdf, "application/pdf", $"recibo-{run.RunNumber}-{entry.EmployeeNumber}.pdf");
    }

    [HttpGet("runs/{id:guid}/ccss-report")]
    public async Task<IActionResult> CcssReport(Guid id, CancellationToken ct)
    {
        var run = await mediator.Send(new GetPayrollRunQuery(id), ct);
        if (run is null) return NotFound();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Planilla CCSS");

        // Header row — columns match CCSS Declaración y Pago format
        var headers = new[]
        {
            "Cédula Trabajador", "Nombre Trabajador", "N° Empleado",
            "Días Trabajados", "Salario Bruto", "Cuota Obrero CCSS",
            "Banco Popular", "Impuesto Renta", "Total Deducciones",
            "Salario Neto", "Cuota Patronal CCSS", "INS Patronal",
            "FCL (3%)", "Total Costo Patronal"
        };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1e3a5f");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        var periodDays = run.PeriodType == "Monthly" ? 30 : 15;
        var row = 2;
        foreach (var e in run.Entries)
        {
            ws.Cell(row, 1).Value  = e.EmployeeNumber; // cedula placeholder
            ws.Cell(row, 2).Value  = e.EmployeeName;
            ws.Cell(row, 3).Value  = e.EmployeeNumber;
            ws.Cell(row, 4).Value  = periodDays;
            ws.Cell(row, 5).Value  = e.TotalGross;
            ws.Cell(row, 6).Value  = e.CcssEmployee;
            ws.Cell(row, 7).Value  = e.BancoPopular;
            ws.Cell(row, 8).Value  = e.IncomeTax;
            ws.Cell(row, 9).Value  = e.TotalDeductions;
            ws.Cell(row, 10).Value = e.NetPay;
            ws.Cell(row, 11).Value = e.CcssEmployer;
            ws.Cell(row, 12).Value = e.InsEmployer;
            ws.Cell(row, 13).Value = e.Fcl;
            ws.Cell(row, 14).Value = e.TotalLaborCost;

            // Format money columns
            var moneyFmt = "#,##0.00";
            foreach (var col in new[] { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 })
                ws.Cell(row, col).Style.NumberFormat.Format = moneyFmt;

            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTALES";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 5).Value  = run.TotalGross;
        ws.Cell(row, 9).Value  = run.TotalDeductions;
        ws.Cell(row, 10).Value = run.TotalNet;
        ws.Cell(row, 14).Value = run.TotalEmployerCost;
        for (var col = 5; col <= 14; col++)
            ws.Cell(row, col).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileName = $"planilla-ccss-{run.RunNumber}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}

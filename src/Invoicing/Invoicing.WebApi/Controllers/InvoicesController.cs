using Microsoft.AspNetCore.Mvc;
using System;

namespace Invoicing.WebApi.Controllers;

[ApiController]
[Route("api/invoicing")]
public class InvoicesController : ControllerBase
{
    [HttpPost("convert")]
    public IActionResult Convert([FromBody] ConvertRequest req)
    {
        // Esqueleto: en una implementación real se llamaría al handler de aplicación
        return StatusCode(501, new { message = "Not implemented: Convert SalesOrder to Invoice", salesOrderId = req.SalesOrderId });
    }

    public class ConvertRequest { public Guid SalesOrderId { get; set; } }
}

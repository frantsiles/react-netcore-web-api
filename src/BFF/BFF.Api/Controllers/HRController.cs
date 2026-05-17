using BFF.Application.HR;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BFF.Api.Controllers;

[ApiController]
[Route("bff/hr")]
[Authorize]
public class HRController(IMediator mediator) : ControllerBase
{
    // ── Departments ────────────────────────────────────────────────────────────

    [HttpGet("departments")]
    public async Task<IActionResult> ListDepartments([FromQuery] bool? isActive, CancellationToken ct)
        => Ok(await mediator.Send(new ListDepartmentsBffQuery(GetToken(), isActive), ct));

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDeptBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateDepartmentBffCommand(
            GetToken(), req.Code, req.Name, req.ParentDepartmentId, req.CostCenter), ct);
        return Created(string.Empty, result);
    }

    // ── Employees ──────────────────────────────────────────────────────────────

    [HttpGet("employees")]
    public async Task<IActionResult> ListEmployees(
        [FromQuery] Guid? departmentId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await mediator.Send(new ListEmployeesBffQuery(GetToken(), departmentId, status), ct));

    [HttpPost("employees")]
    public async Task<IActionResult> Hire([FromBody] HireEmployeeBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new HireEmployeeBffCommand(
            GetToken(), req.FirstName, req.LastName, req.Email, req.Phone,
            req.DepartmentId, req.JobTitle, req.EmploymentType, req.HireDate,
            req.ManagerEmployeeId), ct);
        return Created(string.Empty, result);
    }

    [HttpPost("employees/{id:guid}/terminate")]
    public async Task<IActionResult> Terminate(
        Guid id, [FromBody] TerminateEmployeeBffRequest req, CancellationToken ct)
        => Ok(await mediator.Send(new TerminateEmployeeBffCommand(
            GetToken(), id, req.TerminationDate, req.Reason), ct));

    // ── Contracts ──────────────────────────────────────────────────────────────

    [HttpGet("employees/{id:guid}/contracts")]
    public async Task<IActionResult> ListContracts(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ListContractsBffQuery(GetToken(), id), ct));

    [HttpPost("employees/{id:guid}/contracts")]
    public async Task<IActionResult> CreateContract(
        Guid id, [FromBody] CreateContractBffRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateContractBffCommand(
            GetToken(), id, req.ContractNumber, req.StartDate,
            req.GrossSalary, req.CurrencyCode, req.EndDate, req.Notes), ct);
        return Created(string.Empty, result);
    }

    private string GetToken()
        => HttpContext.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
}

public record CreateDeptBffRequest(string Code, string Name, Guid? ParentDepartmentId, string? CostCenter);
public record HireEmployeeBffRequest(
    string FirstName, string LastName, string Email, string? Phone,
    Guid DepartmentId, string JobTitle, string EmploymentType, string HireDate,
    string? ManagerEmployeeId);
public record TerminateEmployeeBffRequest(string TerminationDate, string Reason);
public record CreateContractBffRequest(
    string ContractNumber, string StartDate, decimal GrossSalary,
    string CurrencyCode, string? EndDate, string? Notes);

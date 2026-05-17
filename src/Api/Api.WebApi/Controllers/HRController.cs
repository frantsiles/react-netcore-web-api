using HR.Application.Contracts.Commands.CreateContract;
using HR.Application.Contracts.Commands.TerminateContract;
using HR.Application.Contracts.Queries.ListContracts;
using HR.Application.Departments.Commands.CreateDepartment;
using HR.Application.Departments.Commands.UpdateDepartment;
using HR.Application.Departments.Queries.ListDepartments;
using HR.Application.Employees.Commands.HireEmployee;
using HR.Application.Employees.Commands.TerminateEmployee;
using HR.Application.Employees.Commands.UpdateEmployee;
using HR.Application.Employees.Queries.GetEmployeeById;
using HR.Application.Employees.Queries.ListEmployees;
using HR.Domain.Employees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.WebApi.Controllers;

[ApiController]
[Route("api/hr")]
[Authorize]
public class HRController(ISender sender) : ControllerBase
{
    // ── Departments ────────────────────────────────────────────────────────────

    [HttpPost("departments")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentCommand cmd, CancellationToken ct)
        => Ok(await sender.Send(cmd, ct));

    [HttpPut("departments/{id:guid}")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDeptRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateDepartmentCommand(id, req.Name, req.ParentDepartmentId, req.CostCenter), ct));

    [HttpGet("departments")]
    public async Task<IActionResult> ListDepartments([FromQuery] bool? isActive, CancellationToken ct)
        => Ok(await sender.Send(new ListDepartmentsQuery(isActive), ct));

    // ── Employees ──────────────────────────────────────────────────────────────

    [HttpPost("employees")]
    public async Task<IActionResult> Hire([FromBody] HireEmployeeCommand cmd, CancellationToken ct)
    {
        var dto = await sender.Send(cmd, ct);
        return CreatedAtAction(nameof(GetEmployee), new { id = dto.Id }, dto);
    }

    [HttpGet("employees/{id:guid}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetEmployeeByIdQuery(id), ct));

    [HttpGet("employees")]
    public async Task<IActionResult> ListEmployees(
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeStatus? status,
        CancellationToken ct)
        => Ok(await sender.Send(new ListEmployeesQuery(departmentId, status), ct));

    [HttpPut("employees/{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmpRequest req, CancellationToken ct)
        => Ok(await sender.Send(new UpdateEmployeeCommand(
            id, req.FirstName, req.LastName, req.Email, req.Phone,
            req.JobTitle, req.DepartmentId, req.ManagerEmployeeId), ct));

    [HttpPost("employees/{id:guid}/terminate")]
    public async Task<IActionResult> Terminate(Guid id, [FromBody] TerminateEmpRequest req, CancellationToken ct)
        => Ok(await sender.Send(new TerminateEmployeeCommand(id, req.TerminationDate, req.Reason), ct));

    // ── Contracts ──────────────────────────────────────────────────────────────

    [HttpPost("employees/{id:guid}/contracts")]
    public async Task<IActionResult> CreateContract(Guid id, [FromBody] CreateContractRequest req, CancellationToken ct)
        => Ok(await sender.Send(new CreateContractCommand(
            id, req.ContractNumber, req.StartDate, req.GrossSalary,
            req.CurrencyCode, req.EndDate, req.Notes), ct));

    [HttpGet("employees/{id:guid}/contracts")]
    public async Task<IActionResult> ListContracts(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new ListContractsQuery(id), ct));

    [HttpPost("contracts/{contractId:guid}/terminate")]
    public async Task<IActionResult> TerminateContract(Guid contractId, [FromBody] TerminateContractRequest req, CancellationToken ct)
        => Ok(await sender.Send(new TerminateContractCommand(contractId, req.Notes), ct));
}

public record UpdateDeptRequest(string Name, Guid? ParentDepartmentId, string? CostCenter);
public record UpdateEmpRequest(string FirstName, string LastName, string Email, string? Phone, string JobTitle, Guid DepartmentId, string? ManagerEmployeeId);
public record TerminateEmpRequest(DateOnly TerminationDate, string Reason);
public record CreateContractRequest(string ContractNumber, DateOnly StartDate, decimal GrossSalary, string CurrencyCode, DateOnly? EndDate, string? Notes);
public record TerminateContractRequest(string? Notes);

using HR.Domain.Contracts;
using HR.Domain.Employees;

namespace HR.Application.Common.Dtos;

public record EmployeeDto(
    Guid Id,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    Guid DepartmentId,
    string JobTitle,
    EmploymentType EmploymentType,
    EmployeeStatus Status,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    string? TerminationReason,
    string? ManagerEmployeeId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record DepartmentDto(
    Guid Id,
    string Code,
    string Name,
    Guid? ParentDepartmentId,
    string? CostCenter,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record ContractDto(
    Guid Id,
    Guid EmployeeId,
    string ContractNumber,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal GrossSalary,
    string CurrencyCode,
    ContractStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

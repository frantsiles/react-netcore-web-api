namespace BFF.Application.HR;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record DepartmentBffDto(
    Guid Id, string Code, string Name, Guid? ParentDepartmentId,
    string? CostCenter, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public record EmployeeBffDto(
    Guid Id, string EmployeeNumber, string FirstName, string LastName, string FullName,
    string Email, string? Phone, Guid DepartmentId, string JobTitle,
    string EmploymentType, string Status, string HireDate, string? TerminationDate,
    string? TerminationReason, string? ManagerEmployeeId, DateTime CreatedAt, DateTime UpdatedAt);

public record ContractBffDto(
    Guid Id, Guid EmployeeId, string ContractNumber, string StartDate, string? EndDate,
    decimal GrossSalary, string CurrencyCode, string Status, string? Notes,
    DateTime CreatedAt, DateTime UpdatedAt);

// ── Queries ───────────────────────────────────────────────────────────────────

public record ListDepartmentsBffQuery(string Token, bool? IsActive)
    : MediatR.IRequest<IReadOnlyList<DepartmentBffDto>>;

public record ListEmployeesBffQuery(string Token, Guid? DepartmentId, string? Status)
    : MediatR.IRequest<IReadOnlyList<EmployeeBffDto>>;

public record ListContractsBffQuery(string Token, Guid EmployeeId)
    : MediatR.IRequest<IReadOnlyList<ContractBffDto>>;

// ── Commands ──────────────────────────────────────────────────────────────────

public record CreateDepartmentBffCommand(
    string Token, string Code, string Name, Guid? ParentDepartmentId, string? CostCenter)
    : MediatR.IRequest<DepartmentBffDto>;

public record HireEmployeeBffCommand(
    string Token, string FirstName, string LastName, string Email, string? Phone,
    Guid DepartmentId, string JobTitle, string EmploymentType, string HireDate,
    string? ManagerEmployeeId)
    : MediatR.IRequest<EmployeeBffDto>;

public record TerminateEmployeeBffCommand(
    string Token, Guid EmployeeId, string TerminationDate, string Reason)
    : MediatR.IRequest<EmployeeBffDto>;

public record CreateContractBffCommand(
    string Token, Guid EmployeeId, string ContractNumber, string StartDate,
    decimal GrossSalary, string CurrencyCode, string? EndDate, string? Notes)
    : MediatR.IRequest<ContractBffDto>;

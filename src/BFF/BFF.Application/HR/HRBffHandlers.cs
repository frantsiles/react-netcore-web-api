using BFF.Domain.Interfaces;
using MediatR;

namespace BFF.Application.HR;

public class ListDepartmentsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListDepartmentsBffQuery, IReadOnlyList<DepartmentBffDto>>
{
    public async Task<IReadOnlyList<DepartmentBffDto>> Handle(
        ListDepartmentsBffQuery request, CancellationToken ct)
    {
        var url = request.IsActive.HasValue
            ? $"api/hr/departments?isActive={request.IsActive}"
            : "api/hr/departments";
        var result = await apiClient.GetAsync<List<DepartmentBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class ListEmployeesBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListEmployeesBffQuery, IReadOnlyList<EmployeeBffDto>>
{
    public async Task<IReadOnlyList<EmployeeBffDto>> Handle(
        ListEmployeesBffQuery request, CancellationToken ct)
    {
        var parts = new List<string>();
        if (request.DepartmentId.HasValue) parts.Add($"departmentId={request.DepartmentId}");
        if (!string.IsNullOrWhiteSpace(request.Status)) parts.Add($"status={request.Status}");

        var url = parts.Count > 0
            ? $"api/hr/employees?{string.Join("&", parts)}"
            : "api/hr/employees";
        var result = await apiClient.GetAsync<List<EmployeeBffDto>>(url, request.Token, ct);
        return result ?? [];
    }
}

public class ListContractsBffQueryHandler(IApiClient apiClient)
    : IRequestHandler<ListContractsBffQuery, IReadOnlyList<ContractBffDto>>
{
    public async Task<IReadOnlyList<ContractBffDto>> Handle(
        ListContractsBffQuery request, CancellationToken ct)
    {
        var result = await apiClient.GetAsync<List<ContractBffDto>>(
            $"api/hr/employees/{request.EmployeeId}/contracts", request.Token, ct);
        return result ?? [];
    }
}

public class CreateDepartmentBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateDepartmentBffCommand, DepartmentBffDto>
{
    public async Task<DepartmentBffDto> Handle(CreateDepartmentBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            Code               = request.Code,
            Name               = request.Name,
            ParentDepartmentId = request.ParentDepartmentId,
            CostCenter         = request.CostCenter,
        };
        var result = await apiClient.PostAsync<object, DepartmentBffDto>(
            "api/hr/departments", body, request.Token, ct);
        return result!;
    }
}

public class HireEmployeeBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<HireEmployeeBffCommand, EmployeeBffDto>
{
    public async Task<EmployeeBffDto> Handle(HireEmployeeBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            FirstName          = request.FirstName,
            LastName           = request.LastName,
            Email              = request.Email,
            Phone              = request.Phone,
            DepartmentId       = request.DepartmentId,
            JobTitle           = request.JobTitle,
            EmploymentType     = request.EmploymentType,
            HireDate           = request.HireDate,
            ManagerEmployeeId  = request.ManagerEmployeeId,
        };
        var result = await apiClient.PostAsync<object, EmployeeBffDto>(
            "api/hr/employees", body, request.Token, ct);
        return result!;
    }
}

public class TerminateEmployeeBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<TerminateEmployeeBffCommand, EmployeeBffDto>
{
    public async Task<EmployeeBffDto> Handle(TerminateEmployeeBffCommand request, CancellationToken ct)
    {
        var body = new { TerminationDate = request.TerminationDate, Reason = request.Reason };
        var result = await apiClient.PostAsync<object, EmployeeBffDto>(
            $"api/hr/employees/{request.EmployeeId}/terminate", body, request.Token, ct);
        return result!;
    }
}

public class CreateContractBffCommandHandler(IApiClient apiClient)
    : IRequestHandler<CreateContractBffCommand, ContractBffDto>
{
    public async Task<ContractBffDto> Handle(CreateContractBffCommand request, CancellationToken ct)
    {
        var body = new
        {
            ContractNumber = request.ContractNumber,
            StartDate      = request.StartDate,
            GrossSalary    = request.GrossSalary,
            CurrencyCode   = request.CurrencyCode,
            EndDate        = request.EndDate,
            Notes          = request.Notes,
        };
        var result = await apiClient.PostAsync<object, ContractBffDto>(
            $"api/hr/employees/{request.EmployeeId}/contracts", body, request.Token, ct);
        return result!;
    }
}

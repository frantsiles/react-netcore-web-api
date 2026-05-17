using HR.Application.Common.Dtos;
using HR.Domain.Contracts;
using HR.Domain.Departments;
using HR.Domain.Employees;

namespace HR.Application.Common.Mappings;

public static class HRMappingExtensions
{
    public static EmployeeDto ToDto(this Employee e) =>
        new(e.Id, e.EmployeeNumber, e.FirstName, e.LastName, e.FullName,
            e.Email, e.Phone, e.DepartmentId, e.JobTitle, e.EmploymentType,
            e.Status, e.HireDate, e.TerminationDate, e.TerminationReason,
            e.ManagerEmployeeId, e.CreatedAt, e.UpdatedAt);

    public static DepartmentDto ToDto(this Department d) =>
        new(d.Id, d.Code, d.Name, d.ParentDepartmentId, d.CostCenter, d.IsActive, d.CreatedAt, d.UpdatedAt);

    public static ContractDto ToDto(this Contract c) =>
        new(c.Id, c.EmployeeId, c.ContractNumber, c.StartDate, c.EndDate,
            c.GrossSalary, c.CurrencyCode, c.Status, c.Notes, c.CreatedAt, c.UpdatedAt);
}

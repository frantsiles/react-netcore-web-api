using HR.Domain.Repositories;
using HR.Infrastructure.Persistence;
using HR.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHRInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<HrDbContext>(opt =>
            opt.UseInMemoryDatabase("HrDb"));

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        return services;
    }
}

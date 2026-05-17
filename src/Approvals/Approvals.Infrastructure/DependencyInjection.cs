using Approvals.Domain.Repositories;
using Approvals.Infrastructure.Persistence;
using Approvals.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Approvals.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddApprovalsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApprovalsDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
                options.UseInMemoryDatabase("ApprovalsDb");
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IApprovalWorkflowRepository, ApprovalWorkflowRepository>();
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
        return services;
    }
}

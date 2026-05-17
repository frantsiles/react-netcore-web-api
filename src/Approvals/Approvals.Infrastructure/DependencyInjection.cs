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
        services.AddDbContext<ApprovalsDbContext>(opt =>
            opt.UseInMemoryDatabase("ApprovalsDb"));

        services.AddScoped<IApprovalWorkflowRepository, ApprovalWorkflowRepository>();
        services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
        return services;
    }
}

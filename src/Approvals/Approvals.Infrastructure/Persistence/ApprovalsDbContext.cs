using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Approvals.Domain.ApprovalRequests;
using Approvals.Domain.ApprovalWorkflows;
using Microsoft.EntityFrameworkCore;

namespace Approvals.Infrastructure.Persistence;

public class ApprovalsDbContext(
    DbContextOptions<ApprovalsDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApprovalWorkflow>(e =>
        {
            e.ToTable("appr_workflows");
            e.HasKey(w => w.Id);
            e.Property(w => w.EntityType).IsRequired().HasMaxLength(100);
            e.Property(w => w.Name).IsRequired().HasMaxLength(200);
            e.Property(w => w.AmountThreshold).HasPrecision(18, 2);

            e.OwnsMany<WorkflowApprover>("_approvers", a =>
            {
                a.ToTable("appr_workflow_approvers");
                a.HasKey(x => x.Id);
                a.Property(x => x.UserId);
                a.WithOwner().HasForeignKey("WorkflowId");
            });
            e.Navigation("_approvers").UsePropertyAccessMode(PropertyAccessMode.Field);
            ApplyTenantFilter<ApprovalWorkflow>(builder);
        });

        builder.Entity<ApprovalRequest>(e =>
        {
            e.ToTable("appr_requests");
            e.HasKey(r => r.Id);
            e.Property(r => r.EntityType).IsRequired().HasMaxLength(100);
            e.Property(r => r.EntityReference).IsRequired().HasMaxLength(100);
            e.Property(r => r.Amount).HasPrecision(18, 2);
            e.Property(r => r.Notes).HasMaxLength(1000);
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.DecisionNotes).HasMaxLength(1000);
            ApplyTenantFilter<ApprovalRequest>(builder);
        });
    }
}

using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Payroll.Domain.PayrollRuns;

namespace Payroll.Infrastructure.Persistence;

public class PayrollDbContext(
    DbContextOptions<PayrollDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("payroll");

        builder.Entity<PayrollRun>(e =>
        {
            e.ToTable("payroll_runs");
            e.HasKey(x => x.Id);
            e.Property(x => x.RunNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.PeriodType).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.TotalGross).HasPrecision(18, 2);
            e.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            e.Property(x => x.TotalNet).HasPrecision(18, 2);
            e.Property(x => x.TotalEmployerCost).HasPrecision(18, 2);
            e.HasIndex(x => new { x.TenantId, x.PeriodStart });
            e.HasIndex(x => x.RunNumber);

            e.OwnsMany(r => r.Entries, entry =>
            {
                entry.ToTable("payroll_entries");
                entry.WithOwner().HasForeignKey("PayrollRunId");
                entry.HasKey(x => x.Id);
                entry.Property(x => x.EmployeeNumber).IsRequired().HasMaxLength(20);
                entry.Property(x => x.EmployeeName).IsRequired().HasMaxLength(200);
                entry.Property(x => x.BaseSalary).HasPrecision(18, 2);
                entry.Property(x => x.GrossSalary).HasPrecision(18, 2);
                entry.Property(x => x.OvertimePay).HasPrecision(18, 2);
                entry.Property(x => x.CcssEmployee).HasPrecision(18, 2);
                entry.Property(x => x.BancoPopular).HasPrecision(18, 2);
                entry.Property(x => x.IncomeTax).HasPrecision(18, 2);
                entry.Property(x => x.CcssEmployer).HasPrecision(18, 2);
                entry.Property(x => x.InsEmployer).HasPrecision(18, 2);
                entry.Property(x => x.Fcl).HasPrecision(18, 2);
                entry.HasIndex(x => x.EmployeeId);
            });
        });
    }
}

using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using HR.Domain.Contracts;
using HR.Domain.Departments;
using HR.Domain.Employees;
using Microsoft.EntityFrameworkCore;

namespace HR.Infrastructure.Persistence;

public class HrDbContext(
    DbContextOptions<HrDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Contract> Contracts => Set<Contract>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Employee>(e =>
        {
            e.ToTable("hr_employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeNumber).IsRequired().HasMaxLength(20);
            e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.JobTitle).IsRequired().HasMaxLength(200);
            e.Property(x => x.EmploymentType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TerminationReason).HasMaxLength(500);
            e.Property(x => x.ManagerEmployeeId).HasMaxLength(20);
            e.Ignore(x => x.FullName);
            e.HasIndex("TenantId", nameof(Employee.EmployeeNumber)).IsUnique();
            ApplyTenantFilter<Employee>(builder);
        });

        builder.Entity<Department>(e =>
        {
            e.ToTable("hr_departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(20);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.CostCenter).HasMaxLength(50);
            e.HasIndex("TenantId", nameof(Department.Code)).IsUnique();
            ApplyTenantFilter<Department>(builder);
        });

        builder.Entity<Contract>(e =>
        {
            e.ToTable("hr_contracts");
            e.HasKey(x => x.Id);
            e.Property(x => x.ContractNumber).IsRequired().HasMaxLength(40);
            e.Property(x => x.GrossSalary).HasPrecision(18, 2);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(1000);
            ApplyTenantFilter<Contract>(builder);
        });
    }
}

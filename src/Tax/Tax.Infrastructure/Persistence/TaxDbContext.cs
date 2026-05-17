using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tax.Domain.TaxRates;

namespace Tax.Infrastructure.Persistence;

public class TaxDbContext(
    DbContextOptions<TaxDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("tax");

        builder.Entity<TaxRate>(e =>
        {
            e.ToTable("tax_rates");
            e.HasKey(t => t.Id);
            e.Property(t => t.Code).IsRequired().HasMaxLength(20);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.Rate).HasPrecision(8, 4);
            e.Property(t => t.Applicability).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Description).HasMaxLength(500);
            e.HasIndex("TenantId", nameof(TaxRate.Code)).IsUnique();
            ApplyTenantFilter<TaxRate>(builder);
        });
    }
}

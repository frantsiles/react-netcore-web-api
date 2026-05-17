using ControlPlane.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace ControlPlane.Infrastructure.Persistence;

public class ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("controlplane");

        builder.Entity<Tenant>(e =>
        {
            e.ToTable("cp_tenants");
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.Slug).IsRequired().HasMaxLength(60);
            e.Property(t => t.CountryCode).IsRequired().HasMaxLength(2);
            e.Property(t => t.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Plan).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(t => t.Slug).IsUnique();

            // Settings is a computed public view of the private _settings field.
            // EF must not attempt to auto-configure it as a separate relationship.
            e.Ignore(t => t.Settings);

            e.OwnsMany<TenantSetting>("_settings", s =>
            {
                s.ToTable("cp_tenant_settings");
                s.HasKey(ts => ts.Id);
                s.Property(ts => ts.Key).IsRequired().HasMaxLength(100);
                s.Property(ts => ts.Value).IsRequired().HasMaxLength(2000);
                s.WithOwner().HasForeignKey("TenantId");
                s.HasIndex("TenantId", "Key").IsUnique();
            });

            e.Navigation("_settings").UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}

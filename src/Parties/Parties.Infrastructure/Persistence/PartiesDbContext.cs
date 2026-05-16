using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Parties.Domain.Parties;

namespace Parties.Infrastructure.Persistence;

public class PartiesDbContext(
    DbContextOptions<PartiesDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Party> Parties => Set<Party>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Party>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.LegalName).IsRequired().HasMaxLength(300);
            entity.Property(p => p.TradeName).HasMaxLength(300);
            entity.Property(p => p.CountryCode).IsRequired().HasMaxLength(2);
            entity.Property(p => p.PartyType).HasConversion<string>();
            entity.Property(p => p.TenantId).IsRequired();

            entity.HasIndex(p => new { p.TenantId, p.IsActive });

            entity.OwnsOne(p => p.TaxId, tax =>
            {
                tax.Property(t => t.Value).HasColumnName("TaxId_Value").HasMaxLength(50);
                tax.Property(t => t.CountryCode).HasColumnName("TaxId_CountryCode").HasMaxLength(2);
            });

            entity.OwnsMany(p => p.Addresses, addr =>
            {
                addr.WithOwner().HasForeignKey("PartyId");
                addr.Property(a => a.Line1).IsRequired().HasMaxLength(250);
                addr.Property(a => a.Line2).HasMaxLength(250);
                addr.Property(a => a.City).IsRequired().HasMaxLength(100);
                addr.Property(a => a.StateOrRegion).IsRequired().HasMaxLength(100);
                addr.Property(a => a.PostalCode).IsRequired().HasMaxLength(20);
                addr.Property(a => a.CountryCode).IsRequired().HasMaxLength(2);
            });

            entity.OwnsMany(p => p.ContactPoints, cp =>
            {
                cp.WithOwner().HasForeignKey("PartyId");
                cp.Property(c => c.Type).HasConversion<string>();
                cp.Property(c => c.Value).IsRequired().HasMaxLength(300);
            });

            entity.OwnsMany(p => p.Roles, role =>
            {
                role.WithOwner().HasForeignKey("PartyId");
                role.HasKey(r => r.Id);
                role.Property(r => r.RoleType).HasConversion<string>();
                role.Property(r => r.Status).HasConversion<string>();

                role.OwnsOne(r => r.CreditLimit, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("CreditLimit_Amount");
                    money.Property(m => m.CurrencyCode).HasColumnName("CreditLimit_Currency").HasMaxLength(3);
                });
            });
        });

        ApplyTenantFilter<Party>(builder);
    }
}

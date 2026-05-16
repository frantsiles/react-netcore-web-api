using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Catalog.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class CatalogDbContext(
    DbContextOptions<CatalogDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CatalogItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SKU).IsRequired().HasMaxLength(50);
            e.Property(x => x.Name).IsRequired().HasMaxLength(300);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.ItemType).HasConversion<string>();
            e.Property(x => x.TaxCategory).IsRequired().HasMaxLength(20);
            e.Property(x => x.DefaultCurrency).IsRequired().HasMaxLength(3);
            e.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
            e.Property(x => x.TenantId).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.SKU }).IsUnique();

            e.OwnsOne(x => x.UnitOfMeasure, uom =>
                uom.Property(u => u.Code).HasColumnName("UnitOfMeasure").IsRequired().HasMaxLength(10));
        });

        builder.Entity<PriceList>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.TenantId).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.IsDefault });

            e.OwnsMany(x => x.Entries, entry =>
            {
                entry.WithOwner().HasForeignKey("PriceListId");
                entry.HasKey(x => x.Id);
                entry.Property(x => x.CatalogItemId).IsRequired();
                entry.Property(x => x.MinQuantity).IsRequired();

                entry.OwnsOne(x => x.UnitPrice, money =>
                {
                    money.Property(m => m.Amount).HasColumnName("UnitPrice_Amount");
                    money.Property(m => m.CurrencyCode).HasColumnName("UnitPrice_Currency").HasMaxLength(3);
                });
            });
        });

        ApplyTenantFilter<CatalogItem>(builder);
        ApplyTenantFilter<PriceList>(builder);
    }
}

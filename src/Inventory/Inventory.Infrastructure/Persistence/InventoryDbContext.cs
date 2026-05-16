using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Inventory.Domain.Inventory;
using Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Warehouse>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).IsRequired().HasMaxLength(20);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TenantId).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        });

        builder.Entity<InventoryItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SKU).IsRequired().HasMaxLength(50);
            e.Property(x => x.TenantId).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.CatalogItemId, x.WarehouseId }).IsUnique();

            e.OwnsMany(x => x.Movements, mv =>
            {
                mv.WithOwner().HasForeignKey("InventoryItemId");
                mv.HasKey(x => x.Id);
                mv.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
                mv.Property(x => x.ReferenceNumber).HasMaxLength(100);
                mv.Property(x => x.Reason).HasMaxLength(300);
                mv.Property(x => x.Notes).HasMaxLength(500);
                mv.Property(x => x.OccurredAt).IsRequired();
            });
        });

        ApplyTenantFilter<Warehouse>(builder);
        ApplyTenantFilter<InventoryItem>(builder);
    }
}

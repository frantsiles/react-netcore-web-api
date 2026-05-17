using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Purchasing.Domain.PurchaseOrders;

namespace Purchasing.Infrastructure.Persistence;

public class PurchasingDbContext(
    DbContextOptions<PurchasingDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("purchasing");

        builder.Entity<PurchaseOrder>(e =>
        {
            e.ToTable("purchasing_purchase_orders");
            e.HasKey(p => p.Id);
            e.Property(p => p.PoNumber).IsRequired().HasMaxLength(30);
            e.Property(p => p.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(p => p.CountryCode).IsRequired().HasMaxLength(2);
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
            e.HasIndex("TenantId", nameof(PurchaseOrder.PoNumber)).IsUnique();

            e.OwnsOne(p => p.Subtotal, m =>
            {
                m.Property(x => x.Amount).HasColumnName("subtotal_amount");
                m.Property(x => x.CurrencyCode).HasColumnName("subtotal_currency").HasMaxLength(3);
            });
            e.OwnsOne(p => p.Total, m =>
            {
                m.Property(x => x.Amount).HasColumnName("total_amount");
                m.Property(x => x.CurrencyCode).HasColumnName("total_currency").HasMaxLength(3);
            });

            e.OwnsMany(p => p.Lines, l =>
            {
                l.ToTable("purchasing_po_lines");
                l.HasKey(x => x.Id);
                l.Property(x => x.Sku).IsRequired().HasMaxLength(50);
                l.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
                l.WithOwner().HasForeignKey("PurchaseOrderId");

                l.OwnsOne(x => x.UnitCost, m =>
                {
                    m.Property(v => v.Amount).HasColumnName("unit_cost_amount");
                    m.Property(v => v.CurrencyCode).HasColumnName("unit_cost_currency").HasMaxLength(3);
                });
                l.OwnsOne(x => x.LineTotal, m =>
                {
                    m.Property(v => v.Amount).HasColumnName("line_total_amount");
                    m.Property(v => v.CurrencyCode).HasColumnName("line_total_currency").HasMaxLength(3);
                });
            });
            ApplyTenantFilter<PurchaseOrder>(builder);
        });
    }
}

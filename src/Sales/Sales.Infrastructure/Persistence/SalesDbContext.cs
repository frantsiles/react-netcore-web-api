using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Sales.Domain.Orders;
using Sales.Domain.Quotes;

namespace Sales.Infrastructure.Persistence;

public class SalesDbContext(
    DbContextOptions<SalesDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("sales");

        builder.Entity<Quote>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.QuoteNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.TenantId).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.QuoteNumber }).IsUnique();

            e.OwnsOne(x => x.Subtotal, m =>
            {
                m.Property(p => p.Amount).HasColumnName("Subtotal_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("Subtotal_Currency").HasMaxLength(3);
            });
            e.OwnsOne(x => x.TaxAmount, m =>
            {
                m.Property(p => p.Amount).HasColumnName("TaxAmount_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("TaxAmount_Currency").HasMaxLength(3);
            });
            e.OwnsOne(x => x.Total, m =>
            {
                m.Property(p => p.Amount).HasColumnName("Total_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("Total_Currency").HasMaxLength(3);
            });

            e.OwnsMany(x => x.Lines, line =>
            {
                line.WithOwner().HasForeignKey("QuoteId");
                line.HasKey(x => x.Id);
                line.Property(x => x.SKU).IsRequired().HasMaxLength(50);
                line.Property(x => x.ItemName).IsRequired().HasMaxLength(300);
                line.Property(x => x.Notes).HasMaxLength(500);
                line.OwnsOne(x => x.UnitPrice, m =>
                {
                    m.Property(p => p.Amount).HasColumnName("UnitPrice_Amount");
                    m.Property(p => p.CurrencyCode).HasColumnName("UnitPrice_Currency").HasMaxLength(3);
                });
                line.OwnsOne(x => x.LineTotal, m =>
                {
                    m.Property(p => p.Amount).HasColumnName("LineTotal_Amount");
                    m.Property(p => p.CurrencyCode).HasColumnName("LineTotal_Currency").HasMaxLength(3);
                });
            });
        });

        builder.Entity<SalesOrder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OrderNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.TenantId).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique();

            e.OwnsOne(x => x.Subtotal, m =>
            {
                m.Property(p => p.Amount).HasColumnName("Subtotal_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("Subtotal_Currency").HasMaxLength(3);
            });
            e.OwnsOne(x => x.TaxAmount, m =>
            {
                m.Property(p => p.Amount).HasColumnName("TaxAmount_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("TaxAmount_Currency").HasMaxLength(3);
            });
            e.OwnsOne(x => x.Total, m =>
            {
                m.Property(p => p.Amount).HasColumnName("Total_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("Total_Currency").HasMaxLength(3);
            });

            e.OwnsMany(x => x.Lines, line =>
            {
                line.WithOwner().HasForeignKey("OrderId");
                line.HasKey(x => x.Id);
                line.Property(x => x.SKU).IsRequired().HasMaxLength(50);
                line.Property(x => x.ItemName).IsRequired().HasMaxLength(300);
                line.Property(x => x.Notes).HasMaxLength(500);
                line.OwnsOne(x => x.UnitPrice, m =>
                {
                    m.Property(p => p.Amount).HasColumnName("UnitPrice_Amount");
                    m.Property(p => p.CurrencyCode).HasColumnName("UnitPrice_Currency").HasMaxLength(3);
                });
                line.OwnsOne(x => x.LineTotal, m =>
                {
                    m.Property(p => p.Amount).HasColumnName("LineTotal_Amount");
                    m.Property(p => p.CurrencyCode).HasColumnName("LineTotal_Currency").HasMaxLength(3);
                });
            });
        });

        ApplyTenantFilter<Quote>(builder);
        ApplyTenantFilter<SalesOrder>(builder);
    }
}

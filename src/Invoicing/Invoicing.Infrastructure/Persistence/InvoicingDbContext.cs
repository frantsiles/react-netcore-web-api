using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Invoicing.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Infrastructure.Persistence;

public class InvoicingDbContext(
    DbContextOptions<InvoicingDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("invoicing");

        builder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.TenantId).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.InvoiceNumber }).IsUnique();

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
            e.OwnsOne(x => x.TotalAmount, m =>
            {
                m.Property(p => p.Amount).HasColumnName("TotalAmount_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("TotalAmount_Currency").HasMaxLength(3);
            });
            e.OwnsOne(x => x.PaidAmount, m =>
            {
                m.Property(p => p.Amount).HasColumnName("PaidAmount_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("PaidAmount_Currency").HasMaxLength(3);
            });
            e.OwnsOne(x => x.BalanceDue, m =>
            {
                m.Property(p => p.Amount).HasColumnName("BalanceDue_Amount");
                m.Property(p => p.CurrencyCode).HasColumnName("BalanceDue_Currency").HasMaxLength(3);
            });

            e.OwnsMany(x => x.Lines, line =>
            {
                line.WithOwner().HasForeignKey("InvoiceId");
                line.HasKey(x => x.Id);
                line.Property(x => x.SKU).HasMaxLength(50);
                line.Property(x => x.Description).IsRequired().HasMaxLength(500);
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

        ApplyTenantFilter<Invoice>(builder);
    }
}

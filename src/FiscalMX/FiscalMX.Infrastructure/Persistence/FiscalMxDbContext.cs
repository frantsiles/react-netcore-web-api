using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using FiscalMX.Domain.CfdiDocuments;
using Microsoft.EntityFrameworkCore;

namespace FiscalMX.Infrastructure.Persistence;

public class FiscalMxDbContext(
    DbContextOptions<FiscalMxDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<CfdiDocument> CfdiDocuments => Set<CfdiDocument>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("fiscal_mx");

        builder.Entity<CfdiDocument>(e =>
        {
            e.ToTable("cfdi_documents");
            e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            e.Property(x => x.Uuid).HasMaxLength(36);
            e.Property(x => x.Serie).HasMaxLength(25);
            e.Property(x => x.Folio).HasMaxLength(40);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.XmlOriginal).HasColumnType("text");
            e.Property(x => x.XmlTimbrado).HasColumnType("text");
            e.Property(x => x.PacMensaje).HasMaxLength(1000);
            e.Property(x => x.CancelMotivo).HasMaxLength(100);
            e.HasIndex(x => x.InvoiceId).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Status });
            e.HasIndex(x => x.Uuid);
        });
    }
}

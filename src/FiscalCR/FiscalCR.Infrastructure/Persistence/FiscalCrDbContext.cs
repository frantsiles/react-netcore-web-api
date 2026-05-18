using FiscalCR.Domain.ElectronicDocuments;
using Microsoft.EntityFrameworkCore;

namespace FiscalCR.Infrastructure.Persistence;

public class FiscalCrDbContext(DbContextOptions<FiscalCrDbContext> options) : DbContext(options)
{
    public DbSet<ElectronicDocument> ElectronicDocuments => Set<ElectronicDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("fiscal_cr");

        modelBuilder.Entity<ElectronicDocument>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TenantId).IsRequired();
            e.Property(x => x.InvoiceId).IsRequired();
            e.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Clave).HasMaxLength(50).IsRequired();
            e.Property(x => x.NumeroConsecutivo).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.XmlUnsigned).HasColumnType("text");
            e.Property(x => x.XmlSigned).HasColumnType("text");
            e.Property(x => x.HaciendaMensaje).HasMaxLength(500);
            e.Property(x => x.HaciendaXmlRespuesta).HasColumnType("text");
            e.Property(x => x.HaciendaEstado).HasMaxLength(20);
            e.HasIndex(x => x.Clave).IsUnique();
            e.HasIndex(x => x.InvoiceId);
            e.HasIndex(x => new { x.TenantId, x.Status });
        });
    }
}

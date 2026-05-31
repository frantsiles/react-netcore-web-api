using FiscalCR.Domain.ElectronicDocuments;
using FiscalCR.Domain.TenantConfig;
using Microsoft.EntityFrameworkCore;

namespace FiscalCR.Infrastructure.Persistence;

public class FiscalCrDbContext(DbContextOptions<FiscalCrDbContext> options) : DbContext(options)
{
    public DbSet<ElectronicDocument> ElectronicDocuments => Set<ElectronicDocument>();
    public DbSet<TenantFiscalCrConfig> TenantConfigs => Set<TenantFiscalCrConfig>();

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

        modelBuilder.Entity<TenantFiscalCrConfig>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TenantId).IsRequired();
            e.Property(x => x.RazonSocial).HasMaxLength(100).IsRequired();
            e.Property(x => x.NombreComercial).HasMaxLength(100).IsRequired();
            e.Property(x => x.TipoIdentificacion).HasMaxLength(2).IsRequired();
            e.Property(x => x.NumeroIdentificacion).HasMaxLength(12).IsRequired();
            e.Property(x => x.CodigoActividad).HasMaxLength(6).IsRequired();
            e.Property(x => x.Provincia).HasMaxLength(1).IsRequired();
            e.Property(x => x.Canton).HasMaxLength(2).IsRequired();
            e.Property(x => x.Distrito).HasMaxLength(2).IsRequired();
            e.Property(x => x.OtrasSenas).HasMaxLength(200).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.Property(x => x.HaciendaUsername).HasMaxLength(100);
            e.Property(x => x.HaciendaPassword).HasMaxLength(200);
            e.Property(x => x.CertificateBytes).HasColumnType("bytea");
            e.Property(x => x.CertificatePassword).HasMaxLength(200);
            e.Property(x => x.Environment).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.TenantId).IsUnique();
        });
    }
}

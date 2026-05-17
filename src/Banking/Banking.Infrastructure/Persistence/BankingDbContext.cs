using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Banking.Domain.BankAccounts;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infrastructure.Persistence;

public class BankingDbContext(
    DbContextOptions<BankingDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BankAccount>(e =>
        {
            e.ToTable("bank_accounts");
            e.HasKey(a => a.Id);
            e.Property(a => a.AccountNumber).IsRequired().HasMaxLength(50);
            e.Property(a => a.BankName).IsRequired().HasMaxLength(200);
            e.Property(a => a.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Iban).HasMaxLength(34);
            e.Property(a => a.Swift).HasMaxLength(11);
            e.HasIndex("TenantId", nameof(BankAccount.AccountNumber)).IsUnique();

            e.OwnsMany<BankTransaction>("_transactions", t =>
            {
                t.ToTable("bank_transactions");
                t.HasKey(x => x.Id);
                t.Property(x => x.Description).IsRequired().HasMaxLength(500);
                t.Property(x => x.Type).HasConversion<string>().HasMaxLength(10);
                t.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                t.Property(x => x.ReferenceNumber).HasMaxLength(100);
                t.WithOwner().HasForeignKey("BankAccountId");
            });

            e.Navigation("_transactions").UsePropertyAccessMode(PropertyAccessMode.Field);
            ApplyTenantFilter<BankAccount>(builder);
        });
    }
}

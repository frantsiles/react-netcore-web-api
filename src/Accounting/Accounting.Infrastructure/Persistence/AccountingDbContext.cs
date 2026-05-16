using Accounting.Domain.Accounts;
using Accounting.Domain.JournalEntries;
using Api.Application.Common.Interfaces;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Persistence;

public class AccountingDbContext(
    DbContextOptions<AccountingDbContext> options,
    ITenantContext tenantContext)
    : TenantAwareDbContext(options, tenantContext)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Account>(e =>
        {
            e.ToTable("acc_accounts");
            e.HasKey(a => a.Id);
            e.Property(a => a.AccountNumber).IsRequired().HasMaxLength(20);
            e.Property(a => a.Name).IsRequired().HasMaxLength(200);
            e.Property(a => a.CurrencyCode).IsRequired().HasMaxLength(3);
            e.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
            e.HasIndex("TenantId", nameof(Account.AccountNumber)).IsUnique();
            ApplyTenantFilter<Account>(builder);
        });

        builder.Entity<JournalEntry>(e =>
        {
            e.ToTable("acc_journal_entries");
            e.HasKey(j => j.Id);
            e.Property(j => j.EntryNumber).IsRequired().HasMaxLength(40);
            e.Property(j => j.FiscalPeriod).IsRequired().HasMaxLength(7);
            e.Property(j => j.Description).IsRequired().HasMaxLength(500);
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex("TenantId", nameof(JournalEntry.EntryNumber)).IsUnique();

            e.OwnsMany<JournalEntryLine>("_lines", l =>
            {
                l.ToTable("acc_journal_entry_lines");
                l.HasKey(x => x.Id);
                l.Property(x => x.AccountNumber).IsRequired().HasMaxLength(20);
                l.Property(x => x.AccountName).IsRequired().HasMaxLength(200);
                l.Property(x => x.Side).HasConversion<string>().HasMaxLength(10);
                l.WithOwner().HasForeignKey("JournalEntryId");
            });

            e.Navigation("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);
            ApplyTenantFilter<JournalEntry>(builder);
        });
    }
}

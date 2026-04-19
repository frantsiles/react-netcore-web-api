using Api.Domain.Permissions;
using Api.Domain.Roles;
using Api.Domain.Users;
using Api.Domain.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext using InMemory provider for local/Codespaces dev.
/// In production this would be replaced with a SQL Server or PostgreSQL provider
/// by swapping the registration in DependencyInjection.cs.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(100);

            // Map Email value object to a single column
            entity.OwnsOne(u => u.Email, email =>
            {
                email.Property(e => e.Value)
                     .HasColumnName("Email")
                     .IsRequired()
                     .HasMaxLength(256);
            });

            // Map PasswordHash value object to a single column
            entity.OwnsOne(u => u.PasswordHash, pw =>
            {
                pw.Property(p => p.Value)
                  .HasColumnName("PasswordHash")
                  .IsRequired();
            });

            entity.HasMany(u => u.Roles)
                  .WithMany()
                  .UsingEntity("UserRoles");
        });

        // Role configuration
        builder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.HasMany(r => r.Permissions)
                  .WithMany()
                  .UsingEntity("RolePermissions");
        });

        // Permission configuration
        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Resource).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Action).IsRequired().HasMaxLength(50);
        });
    }
}

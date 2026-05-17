using Api.Domain.Permissions;
using Api.Domain.Roles;
using Api.Domain.Users;
using Api.Infrastructure.Persistence;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the in-memory database with demo data on application startup.
/// Simulates the initial state of a production database for development/demo purposes.
///
/// Default users:
///   admin@demo.com  / Admin123!   → role: Admin  (all permissions)
///   user@demo.com   / User123!    → role: Viewer (read-only permissions)
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        // --- Permissions ---
        var usersRead    = Permission.Create("users",   "read",   "Read users list");
        var usersWrite   = Permission.Create("users",   "write",  "Create and update users");
        var usersDelete  = Permission.Create("users",   "delete", "Delete users");
        var rolesManage  = Permission.Create("roles",   "manage", "Create and assign roles");

        await context.Permissions.AddRangeAsync(usersRead, usersWrite, usersDelete, rolesManage);

        // --- Roles ---
        var adminRole = Role.Create("Admin", "Full access to all resources");
        adminRole.AddPermission(usersRead);
        adminRole.AddPermission(usersWrite);
        adminRole.AddPermission(usersDelete);
        adminRole.AddPermission(rolesManage);

        var viewerRole = Role.Create("Viewer", "Read-only access");
        viewerRole.AddPermission(usersRead);

        await context.Roles.AddRangeAsync(adminRole, viewerRole);

        // --- Users ---
        var defaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var adminHash  = BCrypt.Net.BCrypt.HashPassword("Admin123!");
        var adminUser  = User.Create("Admin", "Demo", "admin@demo.com", adminHash, defaultTenantId, "US");
        adminUser.AssignRole(adminRole);

        var viewerHash = BCrypt.Net.BCrypt.HashPassword("User123!");
        var viewerUser = User.Create("Viewer", "Demo", "user@demo.com", viewerHash, defaultTenantId, "US");
        viewerUser.AssignRole(viewerRole);

        await context.Users.AddRangeAsync(adminUser, viewerUser);

        await context.SaveChangesAsync();
    }
}

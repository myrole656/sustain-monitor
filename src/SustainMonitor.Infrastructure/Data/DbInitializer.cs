using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Catalogue;

namespace SustainMonitor.Infrastructure.Data;

public static class DbInitializer
{
    /// <summary>
    /// Creates or migrates the SQLite database, switches it to WAL mode, seeds the GBI catalogue,
    /// the four roles and (optionally) a first admin from configuration.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync();
        else
        {
            await db.Database.EnsureCreatedAsync(); // before the first migration is added
            // EnsureCreated never alters an existing file: add tables introduced after a test database was made.
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "ProjectChecklist" (
                    "ProjectId" INTEGER NOT NULL,
                    "ItemKey" TEXT NOT NULL,
                    "CheckedById" TEXT NULL,
                    "CheckedAt" TEXT NOT NULL,
                    CONSTRAINT "PK_ProjectChecklist" PRIMARY KEY ("ProjectId", "ItemKey"),
                    CONSTRAINT "FK_ProjectChecklist_Projects_ProjectId" FOREIGN KEY ("ProjectId") REFERENCES "Projects" ("Id") ON DELETE CASCADE
                );
                """);
        }

        // WAL lets the UI read while a re-score job writes. The setting is stored in the file.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

        foreach (var tool in GbiCatalogue.All())
        {
            if (!await db.GbiTools.AnyAsync(t => t.Code == tool.Code && t.Version == tool.Version))
            {
                db.GbiTools.Add(tool);
                log.LogInformation("Seeded GBI tool {Code} v{Version}", tool.Code, tool.Version);
            }
        }
        await db.SaveChangesAsync();

        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));
        }

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            var users = sp.GetRequiredService<UserManager<AppUser>>();
            if (await users.FindByEmailAsync(email) is null)
            {
                var admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "Platform administrator" };
                var created = await users.CreateAsync(admin, password);
                if (created.Succeeded)
                {
                    await users.AddToRoleAsync(admin, Roles.PlatformAdmin);
                    log.LogInformation("Seeded admin {Email}", email);
                }
                else
                {
                    log.LogWarning("Could not seed admin: {Errors}", string.Join("; ", created.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}

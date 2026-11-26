using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Infrastructure.Data;
using SustainMonitor.Infrastructure.Email;

namespace SustainMonitor.Api.Services;

/// <summary>In-app notification + email. Email failures are logged, never block the action.</summary>
public sealed class Notifier(AppDbContext db, UserManager<AppUser> users, IAppEmailSender email, ILogger<Notifier> log)
{
    public async Task NotifyAsync(IEnumerable<string> userIds, string type, string subject, string message, object? data = null)
    {
        var ids = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        if (ids.Count == 0) return;

        var payload = JsonSerializer.Serialize(new { subject, message, data });
        foreach (var id in ids)
            db.AppNotifications.Add(new Notification { UserId = id, Type = type, PayloadJson = payload });
        await db.SaveChangesAsync();

        foreach (var id in ids)
        {
            var u = await users.FindByIdAsync(id);
            if (u?.Email is null) continue;
            try
            {
                await email.SendAsync(u.Email, subject, $"Hello {u.FullName},\n\n{message}\n\n— Sustain Monitor");
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Could not email {Email} about {Type}", u.Email, type);
            }
        }
    }

    /// <summary>A direct email that is not stored as a notification (e.g. a temporary password).</summary>
    public async Task EmailOnlyAsync(string to, string subject, string body)
    {
        try { await email.SendAsync(to, subject, body); }
        catch (Exception ex) { log.LogWarning(ex, "Could not email {Email}", to); }
    }

    public Task<List<string>> CompanyUsersAsync(int companyId, params CompanyRole[] roles) =>
        db.CompanyUsers.AsNoTracking()
            .Where(u => u.CompanyId == companyId && u.IsActive && roles.Contains(u.Role))
            .Select(u => u.UserId)
            .ToListAsync();

    public async Task<List<string>> PlatformAdminsAsync() =>
        (await users.GetUsersInRoleAsync(Roles.PlatformAdmin)).Select(u => u.Id).ToList();
}

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();

        g.MapGet("/", async (bool? unreadOnly, ClaimsPrincipal user, AppDbContext db) =>
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var list = await db.AppNotifications.AsNoTracking()
                .Where(n => n.UserId == id && (unreadOnly != true || n.ReadAt == null))
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();
            return Results.Ok(list.Select(n => new
            {
                n.Id, n.Type, n.CreatedAt, n.ReadAt,
                Content = JsonSerializer.Deserialize<JsonElement>(n.PayloadJson),
            }));
        });

        g.MapPost("/{id:int}/read", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var n = await db.AppNotifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (n is null) return Results.NotFound();
            n.ReadAt ??= DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        g.MapPost("/read-all", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var unread = await db.AppNotifications.Where(x => x.UserId == uid && x.ReadAt == null).ToListAsync();
            foreach (var n in unread) n.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(new { Marked = unread.Count });
        });
    }
}

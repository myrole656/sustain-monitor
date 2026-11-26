using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Api.Services;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Certificates;
using SustainMonitor.Domain.Companies;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

public record ReasonRequest(string Reason);

/// <summary>Platform admin: verify companies and decide e-cert requests.</summary>
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Platform admin").RequireAuthorization("PlatformAdmin");

        g.MapGet("/companies", async (CompanyStatus? status, AppDbContext db) =>
            await db.Companies.AsNoTracking()
                .Where(c => status == null || c.Status == status)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id, c.Name, c.SsmNo, c.CidbGrade, c.Address, c.Phone, c.Status, c.StatusNote, c.CreatedAt,
                    Ceo = c.Users.Where(u => u.Role == CompanyRole.CEO).Select(u => u.FullName).FirstOrDefault(),
                })
                .ToListAsync());

        g.MapPost("/companies/{id:int}/verify", async (int id, ClaimsPrincipal user, AppDbContext db,
            UserManager<AppUser> users, Notifier notifier) =>
        {
            var c = await db.Companies.FindAsync(id);
            if (c is null) return Results.NotFound();
            var wasSuspended = c.Status == CompanyStatus.Suspended;
            CompanyRules.Verify(c, user.UserId(), DateTime.UtcNow);
            db.AuditLogs.Add(new AuditLog { UserId = user.UserId(), Action = "Verify", Entity = nameof(Company), EntityId = c.SsmNo });
            await db.SaveChangesAsync();
            if (wasSuspended) await SetCompanyLockAsync(db, users, c.Id, locked: false);

            await notifier.NotifyAsync(await notifier.CompanyUsersAsync(c.Id, CompanyRole.CEO), "company.verified",
                $"{c.Name} is verified",
                "Your company is verified on Sustain Monitor. You can now add your Project Managers and Superiors.");
            return Results.Ok(new { c.Id, c.Status });
        });

        g.MapPost("/companies/{id:int}/suspend", async (int id, ReasonRequest req, ClaimsPrincipal user, AppDbContext db,
            UserManager<AppUser> users, Notifier notifier) =>
        {
            var c = await db.Companies.FindAsync(id);
            if (c is null) return Results.NotFound();
            CompanyRules.Suspend(c, user.UserId(), req.Reason, DateTime.UtcNow);
            db.AuditLogs.Add(new AuditLog { UserId = user.UserId(), Action = "Suspend", Entity = nameof(Company), EntityId = c.SsmNo });
            await db.SaveChangesAsync();
            await notifier.NotifyAsync(await notifier.CompanyUsersAsync(c.Id, CompanyRole.CEO), "company.suspended",
                $"{c.Name} is suspended", $"Your company was suspended on Sustain Monitor. Reason: {req.Reason}");
            await SetCompanyLockAsync(db, users, c.Id, locked: true);
            return Results.Ok(new { c.Id, c.Status });
        });

        g.MapPost("/companies/{id:int}/reject", async (int id, ReasonRequest req, ClaimsPrincipal user, AppDbContext db, Notifier notifier) =>
        {
            var c = await db.Companies.FindAsync(id);
            if (c is null) return Results.NotFound();
            CompanyRules.Reject(c, user.UserId(), req.Reason, DateTime.UtcNow);
            db.AuditLogs.Add(new AuditLog { UserId = user.UserId(), Action = "Reject", Entity = nameof(Company), EntityId = c.SsmNo });
            await db.SaveChangesAsync();
            await notifier.NotifyAsync(await notifier.CompanyUsersAsync(c.Id, CompanyRole.CEO), "company.rejected",
                $"{c.Name} was not verified", $"Your company registration was not approved. Reason: {req.Reason}");
            return Results.Ok(new { c.Id, c.Status });
        });

        g.MapGet("/ecerts", async (ECertStatus? status, AppDbContext db) =>
            await db.ECertificates.AsNoTracking()
                .Where(c => status == null || c.Status == status)
                .OrderBy(c => c.RequestedAt)
                .Select(c => new
                {
                    c.Id, c.CertNo, c.Status, c.Points, c.Rating, c.ToolCode, c.ToolVersion, c.RequestedAt, c.DecisionNote,
                    Project = c.Project!.Name, ProjectCode = c.Project.Code, Company = c.Project.Company!.Name,
                    c.AssessmentId,
                })
                .ToListAsync());

        g.MapPost("/ecerts/{id:int}/approve", async (int id, ClaimsPrincipal user, AppDbContext db, Notifier notifier) =>
        {
            var cert = await db.ECertificates.FindAsync(id);
            if (cert is null) return Results.NotFound();
            var now = DateTime.UtcNow;
            var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var sequence = await db.ECertificates.CountAsync(c => c.IssuedAt != null && c.IssuedAt >= yearStart) + 1;
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

            // A newer certificate supersedes any earlier one for the same project.
            var older = await db.ECertificates
                .Where(c => c.ProjectId == cert.ProjectId && c.Status == ECertStatus.Issued)
                .ToListAsync();
            foreach (var o in older) ECertificatePolicy.Revoke(o, user.UserId(), $"Superseded by a newer e-cert", now);

            ECertificatePolicy.Approve(cert, user.UserId(), sequence, token, now);
            db.AuditLogs.Add(new AuditLog { UserId = user.UserId(), Action = "Issue e-cert", Entity = nameof(ECertificate), EntityId = cert.CertNo });
            await db.SaveChangesAsync();
            await NotifyCertAsync(db, notifier, cert, "ecert.issued", $"E-cert {cert.CertNo} issued",
                $"Your GBI-aligned e-cert {cert.CertNo} ({cert.Points} points, {cert.Rating}) is issued and valid until {cert.ExpiresAt:yyyy-MM-dd}.");
            return Results.Ok(new { cert.Id, cert.CertNo, cert.Status, cert.IssuedAt, cert.ExpiresAt, VerifyUrl = $"/api/verify/{cert.VerifyToken}" });
        });

        g.MapPost("/ecerts/{id:int}/decline", async (int id, ReasonRequest req, ClaimsPrincipal user, AppDbContext db, Notifier notifier) =>
        {
            var cert = await db.ECertificates.FindAsync(id);
            if (cert is null) return Results.NotFound();
            ECertificatePolicy.Decline(cert, user.UserId(), req.Reason, DateTime.UtcNow);
            await db.SaveChangesAsync();
            await NotifyCertAsync(db, notifier, cert, "ecert.declined", "E-cert request declined",
                $"Your e-cert request was declined. Reason: {req.Reason}");
            return Results.Ok(new { cert.Id, cert.Status, cert.DecisionNote });
        });

        g.MapPost("/ecerts/{id:int}/revoke", async (int id, ReasonRequest req, ClaimsPrincipal user, AppDbContext db, Notifier notifier) =>
        {
            var cert = await db.ECertificates.FindAsync(id);
            if (cert is null) return Results.NotFound();
            ECertificatePolicy.Revoke(cert, user.UserId(), req.Reason, DateTime.UtcNow);
            db.AuditLogs.Add(new AuditLog { UserId = user.UserId(), Action = "Revoke e-cert", Entity = nameof(ECertificate), EntityId = cert.CertNo });
            await db.SaveChangesAsync();
            await NotifyCertAsync(db, notifier, cert, "ecert.revoked", $"E-cert {cert.CertNo} revoked",
                $"Your e-cert {cert.CertNo} was revoked. Reason: {req.Reason}");
            return Results.Ok(new { cert.Id, cert.Status });
        });

        g.MapGet("/users", async (AppDbContext db, UserManager<AppUser> users) =>
        {
            var accounts = await users.Users.AsNoTracking()
                .OrderBy(u => u.Email)
                .Select(u => new { u.Id, u.Email, u.FullName, u.MustChangePassword, u.LockoutEnd, u.CreatedAt })
                .ToListAsync();
            var memberships = await db.CompanyUsers.AsNoTracking()
                .Select(m => new { m.UserId, m.Role, m.IsActive, Company = m.Company!.Name })
                .ToListAsync();
            var byUser = memberships.ToDictionary(m => m.UserId);
            return Results.Ok(accounts.Select(a => new
            {
                a.Id, a.Email, a.FullName, a.MustChangePassword, a.CreatedAt,
                Locked = a.LockoutEnd != null && a.LockoutEnd > DateTimeOffset.UtcNow,
                Membership = byUser.GetValueOrDefault(a.Id),
            }));
        });
    }

    private static async Task SetCompanyLockAsync(AppDbContext db, UserManager<AppUser> users, int companyId, bool locked)
    {
        var ids = await db.CompanyUsers.Where(u => u.CompanyId == companyId && u.IsActive).Select(u => u.UserId).ToListAsync();
        foreach (var id in ids)
        {
            var account = await users.FindByIdAsync(id);
            if (account is not null) await AccountLock.SetAsync(users, account, locked);
        }
    }

    private static async Task NotifyCertAsync(AppDbContext db, Notifier notifier, ECertificate cert, string type, string subject, string message)
    {
        var companyId = await db.Projects.Where(p => p.Id == cert.ProjectId).Select(p => p.CompanyId).SingleAsync();
        await notifier.NotifyAsync(await notifier.CompanyUsersAsync(companyId, CompanyRole.CEO, CompanyRole.ProjectManager),
            type, subject, message, new { cert.Id, cert.ProjectId });
    }
}

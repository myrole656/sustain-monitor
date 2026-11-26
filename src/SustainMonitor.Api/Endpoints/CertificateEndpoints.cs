using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Certificates;
using SustainMonitor.Domain.Security;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

public static class CertificateEndpoints
{
    public const string Disclaimer =
        "GBI-aligned self-assessment e-certificate issued by Sustain Monitor. " +
        "This is not an official Green Building Index certificate.";

    public static void MapCertificateEndpoints(this IEndpointRouteBuilder app)
    {
        // CEO and PM: their project's e-certs.
        app.MapGet("/api/projects/{id:int}/ecerts", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanDownloadCertificate(m.Role)) return MembershipExtensions.NotAllowed("view e-certs");
            if (await db.FindCompanyProjectAsync(id, m) is null) return Results.NotFound();
            return Results.Ok(await db.ECertificates.AsNoTracking()
                .Where(c => c.ProjectId == id)
                .OrderByDescending(c => c.RequestedAt)
                .Select(c => new
                {
                    c.Id, c.CertNo, c.Status, c.Points, c.Rating, c.ToolCode, c.ToolVersion,
                    c.RequestedAt, c.IssuedAt, c.ExpiresAt, c.DecisionNote,
                    VerifyUrl = c.VerifyToken == null ? null : "/api/verify/" + c.VerifyToken,
                })
                .ToListAsync());
        }).WithTags("E-cert").RequireAuthorization();

        // CEO and PM: everything needed to print the certificate.
        app.MapGet("/api/ecerts/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanDownloadCertificate(m.Role)) return MembershipExtensions.NotAllowed("view e-certs");
            var c = await db.ECertificates.AsNoTracking()
                .Include(x => x.Project).ThenInclude(p => p!.Company)
                .SingleOrDefaultAsync(x => x.Id == id && x.Project!.CompanyId == m.CompanyId);
            if (c is null) return Results.NotFound();
            var categories = await db.CategoryScores.AsNoTracking()
                .Where(s => s.AssessmentId == c.AssessmentId)
                .OrderBy(s => s.Category!.SortOrder)
                .Select(s => new { s.Category!.Code, s.Category.Name, s.Points, s.MaxPoints })
                .ToListAsync();
            return Results.Ok(new
            {
                c.Id, c.CertNo, c.Status, PublicStatus = ECertificatePolicy.PublicStatus(c, DateTime.UtcNow),
                Company = c.Project!.Company!.Name, c.Project.Company.SsmNo, c.Project.Company.CidbGrade,
                Project = c.Project.Name, ProjectCode = c.Project.Code, c.Project.Location,
                c.ToolCode, c.ToolVersion, c.Points, c.Rating, c.IssuedAt, c.ExpiresAt, c.VerifyToken,
                Categories = categories, Disclaimer,
            });
        }).WithTags("E-cert").RequireAuthorization();

        // Public: what a tender board sees after scanning the QR code.
        app.MapGet("/api/verify/{token}", async (string token, AppDbContext db) =>
        {
            var c = await db.ECertificates.AsNoTracking()
                .Include(x => x.Project).ThenInclude(p => p!.Company)
                .SingleOrDefaultAsync(x => x.VerifyToken == token);
            if (c is null) return Results.NotFound(new { Status = "Not found" });
            return Results.Ok(new
            {
                Status = ECertificatePolicy.PublicStatus(c, DateTime.UtcNow),
                c.CertNo,
                Company = c.Project!.Company!.Name,
                c.Project.Company.SsmNo,
                Project = c.Project.Name,
                Tool = $"GBI {c.ToolCode} v{c.ToolVersion}",
                c.Points, c.Rating, c.IssuedAt, c.ExpiresAt,
                Disclaimer,
            });
        }).AllowAnonymous().RequireRateLimiting("public").WithTags("E-cert");
    }
}

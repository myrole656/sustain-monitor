using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Companies;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Projects;
using SustainMonitor.Domain.Security;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

public record ChecklistUpdate(List<string> Done);

/// <summary>The five lifecycle phases (Initiation → Closing) from the original Sustain Monitor wizard.</summary>
public static class ChecklistEndpoints
{
    public static void MapChecklistEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/checklist", () => Results.Ok(new
        {
            Phases = Enum.GetValues<ProjectPhase>(),
            Items = PhaseChecklist.Items,
        })).WithTags("Projects").RequireAuthorization();

        var g = app.MapGroup("/api/projects/{id:int}/checklist").WithTags("Projects").RequireAuthorization();

        g.MapGet("/", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (await db.FindCompanyProjectAsync(id, m) is null) return Results.NotFound();
            var done = await db.ProjectChecklist.AsNoTracking().Where(c => c.ProjectId == id)
                .Select(c => new { c.ItemKey, c.CheckedAt }).ToListAsync();
            var keys = done.Select(d => d.ItemKey).ToList();
            return Results.Ok(new
            {
                Done = keys,
                Phases = PhaseChecklist.Progress(keys),
                CurrentPhase = PhaseChecklist.CurrentPhase(keys),
                CanEdit = Permissions.CanSubmitBaseline(m.Role),
            });
        });

        // The Project Manager keeps the checklist up to date; the list sent replaces the ticked set.
        g.MapPut("/", async (int id, ChecklistUpdate req, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanSubmitBaseline(m.Role)) return MembershipExtensions.NotAllowed("update the project checklist");
            CompanyRules.EnsureActive(m.Company);
            var project = await db.FindCompanyProjectAsync(id, m);
            if (project is null) return Results.NotFound();

            var wanted = PhaseChecklist.Normalize(req.Done).ToHashSet();
            var existing = await db.ProjectChecklist.Where(c => c.ProjectId == id).ToListAsync();
            db.ProjectChecklist.RemoveRange(existing.Where(e => !wanted.Contains(e.ItemKey)));
            var have = existing.Select(e => e.ItemKey).ToHashSet();
            db.ProjectChecklist.AddRange(wanted.Where(k => !have.Contains(k))
                .Select(k => new ProjectChecklistEntry { ProjectId = id, ItemKey = k, CheckedById = m.UserId }));
            db.AuditLogs.Add(new AuditLog { UserId = m.UserId, Action = "Update checklist", Entity = nameof(Project), EntityId = project.Code });
            await db.SaveChangesAsync();
            return Results.Ok(new { Done = wanted, Phases = PhaseChecklist.Progress(wanted), CurrentPhase = PhaseChecklist.CurrentPhase(wanted) });
        });
    }
}

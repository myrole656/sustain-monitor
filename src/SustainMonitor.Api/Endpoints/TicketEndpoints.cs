using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Api.Services;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Scoring;
using SustainMonitor.Domain.Security;
using SustainMonitor.Domain.Tickets;
using SustainMonitor.Infrastructure.Data;
using SustainMonitor.Infrastructure.Storage;

namespace SustainMonitor.Api.Endpoints;

public record CreateTicketRequest(int CriterionId, string Title, string? Description, int PointsAtStake,
    TicketType Type = TicketType.Gap, TicketPriority Priority = TicketPriority.Medium, DateOnly? DueDate = null);

public record MoveTicketRequest(TicketStatus To, int? PointsGained, string? Note);

public record CommentRequest(string Body);

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api").WithTags("CO-tickets").RequireAuthorization();

        // The board: tickets by column, with the moves this user's role may make.
        g.MapGet("/projects/{id:int}/tickets", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (await db.FindCompanyProjectAsync(id, m) is null) return Results.NotFound();

            var tickets = await db.Tickets.AsNoTracking()
                .Where(t => t.ProjectId == id)
                .OrderByDescending(t => t.PointsAtStake).ThenBy(t => t.Key)
                .Select(t => new
                {
                    t.Id, t.Key, t.Title, t.Status, t.Priority, t.Type, t.PointsAtStake, t.PointsAwardedOnClose, t.DueDate,
                    Criterion = t.Criterion!.Code, Category = t.Criterion.Category!.Code,
                    Evidence = t.Evidence.Count,
                })
                .ToListAsync();

            var columns = Enum.GetValues<TicketStatus>().Select(s => new
            {
                Status = s,
                Tickets = tickets.Where(t => t.Status == s).Select(t => new
                {
                    t.Id, t.Key, t.Title, t.Priority, t.Type, t.PointsAtStake, t.PointsAwardedOnClose, t.DueDate,
                    t.Criterion, t.Category, t.Evidence,
                    AllowedMoves = TicketWorkflow.NextFor(t.Status, m.Role),
                }),
            });
            return Results.Ok(new { MyRole = m.Role, Columns = columns });
        });

        g.MapPost("/projects/{id:int}/tickets", async (int id, CreateTicketRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanCreateTicket(m.Role)) return MembershipExtensions.NotAllowed("create CO-tickets");
            var project = await db.FindCompanyProjectAsync(id, m);
            if (project is null) return Results.NotFound();
            var current = await db.CurrentAssessmentAsync(id);
            if (current is null) return Results.Problem("Submit the baseline first.", statusCode: StatusCodes.Status409Conflict);

            var tool = await CatalogueEndpoints.LoadToolAsync(db, project.ToolId);
            var openForCriterion = await db.Tickets.AsNoTracking()
                .Where(t => t.ProjectId == id && t.CriterionId == req.CriterionId && t.Status != TicketStatus.Done)
                .ToListAsync();

            var ticket = TicketFactory.Create(project, tool!, current, req.CriterionId, req.Title, req.Description,
                req.PointsAtStake, req.Type, req.Priority, req.DueDate, m.UserId, openForCriterion, DateTime.UtcNow);
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync(); // also saves project.NextTicketNumber
            return Results.Created($"/api/tickets/{ticket.Id}", new { ticket.Id, ticket.Key, ticket.Status, ticket.PointsAtStake });
        });

        g.MapGet("/tickets/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            var t = await db.Tickets.AsNoTracking()
                .Include(x => x.Criterion)
                .Include(x => x.Comments)
                .Include(x => x.History)
                .Include(x => x.Evidence)
                .AsSplitQuery()
                .SingleOrDefaultAsync(x => x.Id == id && x.Project!.CompanyId == m.CompanyId);
            if (t is null) return Results.NotFound();
            var names = await db.CompanyUsers.AsNoTracking().Where(u => u.CompanyId == m.CompanyId)
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);
            string Name(string? id) => id is null ? "" : names.GetValueOrDefault(id) ?? "Former member";
            return Results.Ok(new
            {
                t.Id, t.ProjectId, t.Key, t.Title, t.Description, t.Status, t.Priority, t.Type, t.PointsAtStake, t.PointsAwardedOnClose,
                t.DueDate, t.CreatedAt, t.ClosedAt,
                Reporter = Name(t.ReporterId), Reviewer = Name(t.ReviewerId),
                Criterion = new { t.Criterion!.Code, t.Criterion.Name, t.Criterion.MaxPoints, t.Criterion.ReferenceUrl },
                AllowedMoves = TicketWorkflow.NextFor(t.Status, m.Role),
                Evidence = t.Evidence.OrderBy(e => e.UploadedAt).Select(e => new { e.Id, e.FileName, e.Stage, e.UploadedAt, UploadedBy = Name(e.UploadedBy) }),
                Comments = t.Comments.OrderBy(c => c.CreatedAt).Select(c => new { Author = Name(c.AuthorId), c.Body, c.CreatedAt }),
                History = t.History.OrderBy(h => h.ChangedAt).Select(h => new { h.Field, h.OldValue, h.NewValue, ChangedBy = Name(h.ChangedBy), h.ChangedAt }),
            });
        });

        // PM uploads evidence (GBI DA / CVA documents) to a ticket.
        g.MapPost("/tickets/{id:int}/evidence", async (int id, IFormFile file, AssessmentStage? stage,
            ClaimsPrincipal user, AppDbContext db, IFileStore store, CancellationToken ct) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanUploadEvidence(m.Role)) return MembershipExtensions.NotAllowed("upload evidence");
            var t = await db.Tickets.SingleOrDefaultAsync(x => x.Id == id && x.Project!.CompanyId == m.CompanyId, ct);
            if (t is null) return Results.NotFound();
            if (t.Status is TicketStatus.Done or TicketStatus.InReview)
                return Results.Problem("Evidence can only be added while the ticket is Open or In progress.", statusCode: StatusCodes.Status409Conflict);
            if (file.Length == 0 || file.Length > LocalFileStore.MaxBytes)
                return Results.Problem("File must be between 1 byte and 20 MB.", statusCode: StatusCodes.Status400BadRequest);

            await using var stream = file.OpenReadStream();
            var path = await store.SaveAsync(stream, file.FileName, ct);
            var ev = new Evidence
            {
                ProjectId = t.ProjectId, CriterionId = t.CriterionId, TicketId = t.Id,
                Stage = stage ?? AssessmentStage.SelfCheck,
                FileName = Path.GetFileName(file.FileName), StoragePath = path, UploadedBy = m.UserId,
            };
            db.EvidenceFiles.Add(ev);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/tickets/{id}/evidence/{ev.Id}", new { ev.Id, ev.FileName });
        }).DisableAntiforgery();

        g.MapGet("/tickets/{id:int}/evidence/{evidenceId:int}", async (int id, int evidenceId, ClaimsPrincipal user,
            AppDbContext db, IFileStore store) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            var ev = await db.EvidenceFiles.AsNoTracking()
                .SingleOrDefaultAsync(e => e.Id == evidenceId && e.TicketId == id && e.Project!.CompanyId == m.CompanyId);
            if (ev is null) return Results.NotFound();
            return Results.File(store.OpenRead(ev.StoragePath), "application/octet-stream", ev.FileName);
        });

        // Move a ticket. When a Superior approves (Done), the project is re-scored into a new snapshot.
        g.MapPost("/tickets/{id:int}/move", async (int id, MoveTicketRequest req, ClaimsPrincipal user, AppDbContext db, Notifier notifier) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            var t = await db.Tickets.Include(x => x.Project)
                .SingleOrDefaultAsync(x => x.Id == id && x.Project!.CompanyId == m.CompanyId);
            if (t is null) return Results.NotFound();

            var evidenceCount = await db.EvidenceFiles.CountAsync(e => e.TicketId == id);
            var now = DateTime.UtcNow;
            TicketWorkflow.Move(t, req.To, m.UserId, m.Role, now, evidenceCount, req.PointsGained, req.Note);

            if (t.Status != TicketStatus.Done)
            {
                await db.SaveChangesAsync();
                if (t.Status == TicketStatus.InReview)
                    await notifier.NotifyAsync(await notifier.CompanyUsersAsync(m.CompanyId, CompanyRole.Superior),
                        "ticket.review", $"{t.Key} is ready for review",
                        $"{t.Key} \"{t.Title}\" on {t.Project!.Name} has evidence and is waiting for your review.", new { t.Id });
                else if (t.Status == TicketStatus.InProgress && req.To == TicketStatus.InProgress && m.Role == CompanyRole.Superior)
                    await notifier.NotifyAsync([t.ReporterId], "ticket.rejected", $"{t.Key} was sent back",
                        $"{t.Key} \"{t.Title}\" was rejected by the Superior. Note: {req.Note ?? "(none)"}", new { t.Id });
                return Results.Ok(new { t.Id, t.Key, t.Status });
            }

            // Approved: re-score. Two saves inside one transaction so the old snapshot stops being
            // current before the new one is inserted (one current snapshot per project).
            await using var tx = await db.Database.BeginTransactionAsync();
            var current = await db.CurrentAssessmentAsync(t.ProjectId)
                ?? throw new TicketWorkflowException("Project has no baseline.");
            var tool = await CatalogueEndpoints.LoadToolAsync(db, t.Project!.ToolId);
            var next = TicketFactory.ApplyApproved(t, current, tool!, ProjectContext.From(t.Project));
            await db.SaveChangesAsync();          // ticket Done + old snapshot IsCurrent = false
            db.Assessments.Add(next);
            await db.SaveChangesAsync();          // new snapshot
            await tx.CommitAsync();

            await notifier.NotifyAsync([t.ReporterId], "ticket.approved", $"{t.Key} approved (+{t.PointsAwardedOnClose} points)",
                $"{t.Key} \"{t.Title}\" was approved. Project score is now {next.TotalPoints} ({next.Rating}).", new { t.Id });
            if (current.Rating != next.Rating)
                await notifier.NotifyAsync(await notifier.CompanyUsersAsync(m.CompanyId, CompanyRole.CEO, CompanyRole.ProjectManager),
                    "project.rating", $"{t.Project.Name} moved to {next.Rating}",
                    $"{t.Project.Name} went from {current.TotalPoints} ({current.Rating}) to {next.TotalPoints} ({next.Rating}).",
                    new { t.ProjectId });

            return Results.Ok(new
            {
                t.Id, t.Key, t.Status, t.PointsAwardedOnClose,
                Score = new
                {
                    Before = new { current.VersionNo, current.TotalPoints, current.Rating },
                    After = new { next.VersionNo, next.TotalPoints, next.Rating },
                    RatingChanged = current.Rating != next.Rating,
                },
            });
        });

        g.MapPost("/tickets/{id:int}/comments", async (int id, CommentRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (string.IsNullOrWhiteSpace(req.Body))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = ["Comment is empty."] });
            var exists = await db.Tickets.AnyAsync(x => x.Id == id && x.Project!.CompanyId == m.CompanyId);
            if (!exists) return Results.NotFound();
            var c = new TicketComment { TicketId = id, AuthorId = m.UserId, Body = req.Body.Trim() };
            db.TicketComments.Add(c);
            await db.SaveChangesAsync();
            return Results.Created($"/api/tickets/{id}", new { c.Id, c.CreatedAt });
        });
    }
}

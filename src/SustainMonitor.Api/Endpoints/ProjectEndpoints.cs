using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Api.Services;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Certificates;
using SustainMonitor.Domain.Companies;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Projects;
using SustainMonitor.Domain.Scoring;
using SustainMonitor.Domain.Security;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

public record CreateProjectRequest(
    string Code, string Name, int ToolId, string? Location, string? BuildingType, decimal? GfaM2,
    string? TargetRating, DateOnly? RegDate, string? PicName, string? PicContact, List<string>? Checklist = null);

public record BaselineRequest(AssessmentStage Stage, List<ScoreInputDto> Scores);

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization();

        g.MapGet("/", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            var list = await db.Projects.AsNoTracking()
                .Where(p => p.CompanyId == m.CompanyId)
                .OrderBy(p => p.Name)
                .Select(p => new
                {
                    p.Id, p.Code, p.Name, p.Location, p.TargetRating,
                    Tool = p.Tool!.Code,
                    Current = p.Assessments.Where(a => a.IsCurrent)
                        .Select(a => new { a.VersionNo, a.TotalPoints, a.Rating, a.CreatedAt })
                        .FirstOrDefault(),
                    OpenTickets = p.Tickets.Count(t => t.Status != TicketStatus.Done),
                })
                .ToListAsync();
            return Results.Ok(list);
        });

        g.MapPost("/", async (CreateProjectRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanCreateProject(m.Role)) return MembershipExtensions.NotAllowed("create projects");
            CompanyRules.EnsureActive(m.Company);

            var errors = new Dictionary<string, string[]>();
            var code = (req.Code ?? "").Trim().ToUpperInvariant();
            if (code.Length is < 2 or > 10 || !code.All(char.IsLetterOrDigit))
                errors["code"] = ["Code must be 2–10 letters or digits, e.g. KLT."];
            else if (await db.Projects.AnyAsync(p => p.Code == code))
                errors["code"] = ["Code is already used."];
            if (string.IsNullOrWhiteSpace(req.Name))
                errors["name"] = ["Name is required."];
            if (!await db.GbiTools.AnyAsync(t => t.Id == req.ToolId && t.IsActive))
                errors["toolId"] = ["Unknown GBI tool."];
            IReadOnlyList<string> checklist = [];
            try { checklist = PhaseChecklist.Normalize(req.Checklist); }
            catch (ChecklistException ex) { errors["checklist"] = [ex.Message]; }
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var project = new Project
            {
                CompanyId = m.CompanyId,
                Code = code,
                Name = req.Name.Trim(),
                ToolId = req.ToolId,
                Location = req.Location,
                BuildingType = req.BuildingType,
                GfaM2 = req.GfaM2,
                TargetRating = req.TargetRating,
                RegDate = req.RegDate,
                PicName = req.PicName,
                PicContact = req.PicContact,
                CreatedById = m.UserId,
                Checklist = checklist.Select(k => new ProjectChecklistEntry { ItemKey = k, CheckedById = m.UserId }).ToList(),
            };
            db.Projects.Add(project);
            db.AuditLogs.Add(new AuditLog { UserId = m.UserId, Action = "Create", Entity = nameof(Project), EntityId = code });
            await db.SaveChangesAsync();
            return Results.Created($"/api/projects/{project.Id}", new { project.Id, project.Code, project.Name });
        });

        // Baseline: the PM scores every criterion once. After this, points only change through approved CO-tickets.
        g.MapPost("/{id:int}/baseline", async (int id, BaselineRequest req, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanSubmitBaseline(m.Role)) return MembershipExtensions.NotAllowed("submit the baseline");
            var project = await db.FindCompanyProjectAsync(id, m);
            if (project is null) return Results.NotFound();
            if (await db.Assessments.AnyAsync(a => a.ProjectId == id))
                return Results.Problem("Baseline already submitted. Raise CO-tickets to change scores.", statusCode: StatusCodes.Status409Conflict);

            var tool = await CatalogueEndpoints.LoadToolAsync(db, project.ToolId);
            var inputs = req.Scores.Select(s => new ScoreInput(s.CriterionId, s.Status, s.PointsTargeted, s.PointsAwarded)).ToList();
            var result = ScoringEngine.Evaluate(tool!, inputs, ProjectContext.From(project));
            var v1 = ScoringEngine.ToAssessment(result, project.Id, req.Stage, 1, AssessmentTrigger.Initial, m.UserId);
            db.Assessments.Add(v1);
            await db.SaveChangesAsync();

            var gaps = GapAnalyzer.FindGaps(tool!, v1);
            return Results.Created($"/api/projects/{id}", new
            {
                v1.Id, v1.TotalPoints, v1.Rating,
                SuggestedTickets = gaps.Select(x => new { x.CriterionId, x.CriterionCode, x.CriterionName, x.CategoryCode, x.PointsAtStake }),
            });
        });

        g.MapGet("/{id:int}", async (int id, ClaimsPrincipal user, AppDbContext db, IConfiguration config) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            var project = await db.FindCompanyProjectAsync(id, m);
            if (project is null) return Results.NotFound();
            var tool = await CatalogueEndpoints.LoadToolAsync(db, project.ToolId);
            var current = await db.CurrentAssessmentAsync(id);
            var tickets = await db.Tickets.AsNoTracking().Where(t => t.ProjectId == id).ToListAsync();
            var progress = current is null ? null
                : ProgressCalculator.Calculate(tool!, ProjectContext.From(project), current, tickets, Threshold(config));

            var openStake = tickets.Where(t => t.Status != TicketStatus.Done)
                .GroupBy(t => t.CriterionId).ToDictionary(g => g.Key, g => g.Sum(t => t.PointsAtStake));
            var ticked = await db.ProjectChecklist.AsNoTracking().Where(c => c.ProjectId == id).Select(c => c.ItemKey).ToListAsync();
            var categoryNames = tool!.Categories.ToDictionary(c => c.Id, c => (c.Code, c.Name));
            var categoryOfCriterion = tool.Categories.SelectMany(c => c.Criteria.Select(x => (x.Id, c.Code))).ToDictionary(x => x.Id, x => x.Code);
            var criterionInfo = tool.Categories.SelectMany(c => c.Criteria).ToDictionary(c => c.Id, c => (c.Code, c.Name, c.MaxPoints));
            return Results.Ok(new
            {
                project.Id, project.Code, project.Name, project.Location, project.BuildingType, project.GfaM2,
                project.TargetRating, project.ToolId, Tool = new { tool.Code, tool.Name, tool.Version },
                MyRole = m.Role,
                Progress = progress,
                Phases = PhaseChecklist.Progress(ticked),
                CurrentPhase = PhaseChecklist.CurrentPhase(ticked),
                Current = current is null ? null : new
                {
                    current.Id, current.VersionNo, current.TotalPoints, current.Rating, current.CreatedAt, current.Trigger,
                    Categories = current.CategoryScores.Select(c => new { categoryNames[c.CategoryId].Code, categoryNames[c.CategoryId].Name, c.Points, c.MaxPoints }),
                    Criteria = current.CriterionScores.Select(s => new
                    {
                        s.CriterionId, criterionInfo[s.CriterionId].Code, criterionInfo[s.CriterionId].Name,
                        Category = categoryOfCriterion[s.CriterionId],
                        s.Status, s.PointsTargeted, s.PointsAwarded, Max = criterionInfo[s.CriterionId].MaxPoints,
                        OpenStake = openStake.GetValueOrDefault(s.CriterionId),
                    }),
                },
            });
        });

        g.MapGet("/{id:int}/history", async (int id, ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (await db.FindCompanyProjectAsync(id, m) is null) return Results.NotFound();
            return Results.Ok(await db.Assessments.AsNoTracking()
                .Where(a => a.ProjectId == id)
                .OrderBy(a => a.VersionNo)
                .Select(a => new { a.VersionNo, a.TotalPoints, a.Rating, a.Trigger, a.TriggerTicketId, a.CreatedAt })
                .ToListAsync());
        });

        // CEO or PM, once progress reaches the threshold: run the GBI analysis and request the e-cert if it qualifies.
        g.MapPost("/{id:int}/analysis", async (int id, ClaimsPrincipal user, AppDbContext db, IConfiguration config, Notifier notifier) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            if (!Permissions.CanRunAnalysis(m.Role)) return MembershipExtensions.NotAllowed("run the GBI analysis");
            CompanyRules.EnsureActive(m.Company);
            var project = await db.FindCompanyProjectAsync(id, m);
            if (project is null) return Results.NotFound();
            var current = await db.CurrentAssessmentAsync(id);
            if (current is null) return Results.Problem("Submit the baseline first.", statusCode: StatusCodes.Status409Conflict);

            var tool = await CatalogueEndpoints.LoadToolAsync(db, project.ToolId);
            var ctx = ProjectContext.From(project);
            var tickets = await db.Tickets.AsNoTracking().Where(t => t.ProjectId == id).ToListAsync();
            var progress = ProgressCalculator.Calculate(tool!, ctx, current, tickets, Threshold(config));
            if (!progress.CanRunAnalysis)
                return Results.Problem(
                    $"Progress is {(int)(progress.Progress * 100)}%. The GBI analysis unlocks at {(int)(progress.Threshold * 100)}% of targeted points.",
                    statusCode: StatusCodes.Status409Conflict);

            // Re-evaluate from scratch so the result always reflects the current GBI rules.
            var inputs = current.CriterionScores.Select(s => new ScoreInput(s.CriterionId, s.Status, s.PointsTargeted, s.PointsAwarded));
            var result = ScoringEngine.Evaluate(tool!, inputs, ctx);

            var existing = await db.ECertificates.Where(c => c.ProjectId == id).ToListAsync();
            object certificate;
            var newRequest = false;
            if (existing.Any(c => c.Status == ECertStatus.Requested))
            {
                certificate = new { Status = "AlreadyRequested", Message = "An e-cert request is already waiting for platform admin approval." };
            }
            else
            {
                var request = ECertificatePolicy.Request(tool!, project, current, m.UserId, DateTime.UtcNow, existing);
                if (request is null)
                {
                    certificate = new { Status = "NotEligible", Message = $"{result.TotalPoints} points is below {ECertificatePolicy.MinimumPoints(tool!)}; no e-cert." };
                }
                else
                {
                    db.ECertificates.Add(request);
                    newRequest = true;
                    certificate = new { Status = "Requested", Message = "E-cert requested. A platform admin will review it." };
                }
            }

            db.AuditLogs.Add(new AuditLog { UserId = m.UserId, Action = "Run analysis", Entity = nameof(Project), EntityId = project.Code });
            await db.SaveChangesAsync();
            if (newRequest)
                await notifier.NotifyAsync(await notifier.PlatformAdminsAsync(), "ecert.requested",
                    $"E-cert request: {m.Company.Name} / {project.Name}",
                    $"{m.Company.Name} requested an e-cert for {project.Name}: {result.TotalPoints} points ({result.Rating}).",
                    new { project.Id });

            return Results.Ok(new
            {
                result.TotalPoints, result.MaxPoints, result.Rating,
                Categories = result.Categories,
                Progress = progress,
                Certificate = certificate,
            });
        });
    }

    internal static decimal Threshold(IConfiguration config) =>
        decimal.TryParse(config["Analysis:ProgressThreshold"], System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var t) && t is > 0 and <= 1
            ? t
            : ProgressCalculator.DefaultThreshold;
}

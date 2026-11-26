using Microsoft.EntityFrameworkCore;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Scoring;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

public record ScorePreviewRequest(decimal? GfaM2, List<ScoreInputDto> Scores);
public record ScoreInputDto(int CriterionId, CriterionStatus Status, int PointsTargeted, int PointsAwarded);

public static class CatalogueEndpoints
{
    public static void MapCatalogueEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/tools").WithTags("GBI catalogue").RequireAuthorization();

        g.MapGet("/", async (AppDbContext db) =>
            await db.GbiTools.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Code)
                .Select(t => new { t.Id, t.Code, t.Name, t.Version })
                .ToListAsync());

        g.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var tool = await db.GbiTools.AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id, t.Code, t.Name, t.Version,
                    RatingBands = t.RatingBands.OrderByDescending(b => b.MinPoints)
                        .Select(b => new { b.Name, b.MinPoints, b.MaxPoints }),
                    Categories = t.Categories.OrderBy(c => c.SortOrder).Select(c => new
                    {
                        c.Id, c.Code, c.Name, c.MaxPoints,
                        Criteria = c.Criteria.OrderBy(x => x.SortOrder).Select(x => new
                        {
                            x.Id, x.Code, x.Name, x.MaxPoints, x.ReferenceUrl,
                            Variants = x.Variants.Select(v => new { v.ConditionKey, v.Description, v.MaxPoints }),
                        }),
                    }),
                })
                .SingleOrDefaultAsync();
            return tool is null ? Results.NotFound() : Results.Ok(tool);
        });

        // Score a set of answers without saving anything: useful for the UI's live total.
        g.MapPost("/{id:int}/score-preview", async (int id, ScorePreviewRequest req, AppDbContext db) =>
        {
            var tool = await LoadToolAsync(db, id);
            if (tool is null) return Results.NotFound();
            try
            {
                var inputs = req.Scores.Select(s => new ScoreInput(s.CriterionId, s.Status, s.PointsTargeted, s.PointsAwarded));
                return Results.Ok(ScoringEngine.Evaluate(tool, inputs, new ProjectContext(req.GfaM2)));
            }
            catch (ScoringException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["scores"] = [ex.Message] });
            }
        });
    }

    internal static Task<Domain.Entities.GbiTool?> LoadToolAsync(AppDbContext db, int id) =>
        db.GbiTools.AsNoTracking()
            .Include(t => t.RatingBands)
            .Include(t => t.Categories).ThenInclude(c => c.Criteria).ThenInclude(c => c.Variants)
            .AsSplitQuery()
            .SingleOrDefaultAsync(t => t.Id == id);
}

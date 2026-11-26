using SustainMonitor.Domain.Entities;

namespace SustainMonitor.Domain.Scoring;

/// <summary>Facts about a project that change which criterion variant applies.</summary>
public sealed record ProjectContext(decimal? GfaM2)
{
    public static ProjectContext From(Project p) => new(p.GfaM2);
}

/// <summary>One criterion's answer going into the engine.</summary>
public sealed record ScoreInput(int CriterionId, CriterionStatus Status, int PointsTargeted, int PointsAwarded, string? Note = null);

public sealed record CriterionResult(int CriterionId, string Code, int MaxPoints, CriterionStatus Status, int PointsTargeted, int PointsAwarded);

public sealed record CategoryResult(int CategoryId, string Code, int Points, int MaxPoints);

public sealed record ScoringResult(int TotalPoints, int MaxPoints, string Rating,
    IReadOnlyList<CategoryResult> Categories, IReadOnlyList<CriterionResult> Criteria);

public sealed class ScoringException(string message) : Exception(message);

/// <summary>Works out which max applies to a criterion for a given project.</summary>
public static class VariantResolver
{
    public static int EffectiveMax(Criterion criterion, ProjectContext ctx)
    {
        foreach (var v in criterion.Variants)
        {
            if (Applies(v.ConditionKey, ctx)) return v.MaxPoints;
        }
        return criterion.MaxPoints;
    }

    public static bool Applies(string conditionKey, ProjectContext ctx) => conditionKey switch
    {
        Catalogue.GbiCatalogue.GfaAbove300 => ctx.GfaM2 is > 300m,
        _ => throw new ScoringException($"Unknown variant condition '{conditionKey}'."),
    };
}

/// <summary>
/// The single place where GBI points and ratings are calculated. Pure: no database, no clock,
/// so every rule is unit-tested. The UI and API never add up points themselves.
/// </summary>
public static class ScoringEngine
{
    public static ScoringResult Evaluate(GbiTool tool, IEnumerable<ScoreInput> inputs, ProjectContext ctx)
    {
        var criteria = tool.Categories.SelectMany(c => c.Criteria).ToDictionary(c => c.Id);
        var byId = new Dictionary<int, ScoreInput>();
        foreach (var input in inputs)
        {
            if (!criteria.ContainsKey(input.CriterionId))
                throw new ScoringException($"Criterion {input.CriterionId} is not part of {tool.Code} v{tool.Version}.");
            if (!byId.TryAdd(input.CriterionId, input))
                throw new ScoringException($"Criterion {input.CriterionId} was scored more than once.");
        }

        var criterionResults = new List<CriterionResult>();
        var categoryResults = new List<CategoryResult>();

        foreach (var category in tool.Categories.OrderBy(c => c.SortOrder))
        {
            var catPoints = 0;
            var catMax = 0;
            foreach (var criterion in category.Criteria.OrderBy(c => c.SortOrder))
            {
                var max = VariantResolver.EffectiveMax(criterion, ctx);
                catMax += max;
                var input = byId.GetValueOrDefault(criterion.Id)
                            ?? new ScoreInput(criterion.Id, CriterionStatus.NotStarted, 0, 0);

                Validate(criterion, input, max);

                var awarded = input.Status == CriterionStatus.NotApplicable ? 0 : input.PointsAwarded;
                catPoints += awarded;
                criterionResults.Add(new CriterionResult(criterion.Id, criterion.Code, max, input.Status, input.PointsTargeted, awarded));
            }
            categoryResults.Add(new CategoryResult(category.Id, category.Code, catPoints, catMax));
        }

        var total = categoryResults.Sum(c => c.Points);
        var maxTotal = categoryResults.Sum(c => c.MaxPoints);
        return new ScoringResult(total, maxTotal, RatingFor(tool, total), categoryResults, criterionResults);
    }

    public static string RatingFor(GbiTool tool, int totalPoints) =>
        tool.RatingBands
            .OrderByDescending(b => b.MinPoints)
            .FirstOrDefault(b => totalPoints >= b.MinPoints)?.Name
        ?? RatingNames.NotRated;

    /// <summary>Builds a new snapshot from a scoring result. Not saved; the caller persists it.</summary>
    public static Assessment ToAssessment(ScoringResult result, int projectId, AssessmentStage stage, int versionNo,
        AssessmentTrigger trigger, string? createdBy, int? triggerTicketId = null, IEnumerable<ScoreInput>? notes = null)
    {
        var noteById = notes?.ToDictionary(n => n.CriterionId, n => n.Note) ?? [];
        return new Assessment
        {
            ProjectId = projectId,
            Stage = stage,
            VersionNo = versionNo,
            Trigger = trigger,
            TriggerTicketId = triggerTicketId,
            TotalPoints = result.TotalPoints,
            Rating = result.Rating,
            IsCurrent = true,
            CreatedBy = createdBy,
            CriterionScores = result.Criteria.Select(c => new CriterionScore
            {
                CriterionId = c.CriterionId,
                Status = c.Status,
                PointsTargeted = c.PointsTargeted,
                PointsAwarded = c.PointsAwarded,
                Note = noteById.GetValueOrDefault(c.CriterionId),
            }).ToList(),
            CategoryScores = result.Categories.Select(c => new CategoryScore
            {
                CategoryId = c.CategoryId,
                Points = c.Points,
                MaxPoints = c.MaxPoints,
            }).ToList(),
        };
    }

    /// <summary>
    /// The re-evaluation step run when a CO-ticket is closed: copy the current snapshot,
    /// change only the ticket's criterion, and return the next version.
    /// </summary>
    public static Assessment Rescore(Assessment current, GbiTool tool, ProjectContext ctx,
        int criterionId, int newPointsAwarded, int ticketId, string closedBy)
    {
        if (!current.IsCurrent)
            throw new ScoringException("Only the current assessment can be re-scored.");

        var inputs = current.CriterionScores.Select(s =>
        {
            if (s.CriterionId != criterionId)
                return new ScoreInput(s.CriterionId, s.Status, s.PointsTargeted, s.PointsAwarded, s.Note);

            var status = newPointsAwarded >= s.PointsTargeted && newPointsAwarded > 0
                ? CriterionStatus.Achieved
                : CriterionStatus.InProgress;
            return new ScoreInput(s.CriterionId, status, s.PointsTargeted, newPointsAwarded, s.Note);
        }).ToList();

        if (inputs.All(i => i.CriterionId != criterionId))
            throw new ScoringException($"Criterion {criterionId} is not in assessment {current.Id}.");

        var result = Evaluate(tool, inputs, ctx);
        current.IsCurrent = false;
        return ToAssessment(result, current.ProjectId, current.Stage, current.VersionNo + 1,
            AssessmentTrigger.TicketClosed, closedBy, ticketId, inputs);
    }

    private static void Validate(Criterion criterion, ScoreInput input, int max)
    {
        if (input.PointsAwarded < 0 || input.PointsTargeted < 0)
            throw new ScoringException($"{criterion.Code}: points cannot be negative.");
        if (input.PointsAwarded > max)
            throw new ScoringException($"{criterion.Code}: awarded {input.PointsAwarded} exceeds max {max}.");
        if (input.PointsTargeted > max)
            throw new ScoringException($"{criterion.Code}: targeted {input.PointsTargeted} exceeds max {max}.");
    }
}

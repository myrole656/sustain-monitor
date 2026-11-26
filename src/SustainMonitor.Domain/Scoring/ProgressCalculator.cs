using SustainMonitor.Domain.Entities;

namespace SustainMonitor.Domain.Scoring;

public sealed record ProgressReport(
    int ConfirmedPoints,
    string ConfirmedRating,
    int ProjectedPoints,
    string ProjectedRating,
    int TargetedPoints,
    decimal Progress,
    decimal Threshold,
    bool CanRunAnalysis,
    int OpenTickets,
    int DoneTickets);

/// <summary>
/// Confirmed score = the current snapshot (baseline + approved tickets).
/// Projected score = confirmed + points at stake on tickets not yet Done, capped per criterion max.
/// Progress = confirmed points ÷ targeted points. The GBI analysis unlocks at <see cref="DefaultThreshold"/>.
/// </summary>
public static class ProgressCalculator
{
    public const decimal DefaultThreshold = 0.80m;

    public static ProgressReport Calculate(GbiTool tool, ProjectContext ctx, Assessment current,
        IEnumerable<Ticket> tickets, decimal threshold = DefaultThreshold)
    {
        var criteria = tool.Categories.SelectMany(c => c.Criteria).ToDictionary(c => c.Id);
        var ticketList = tickets.ToList();
        var open = ticketList.Where(t => t.Status != TicketStatus.Done).ToList();

        var stakeByCriterion = open
            .GroupBy(t => t.CriterionId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.PointsAtStake));

        var projected = 0;
        var targeted = 0;
        foreach (var s in current.CriterionScores)
        {
            if (s.Status == CriterionStatus.NotApplicable) continue;
            var max = criteria.TryGetValue(s.CriterionId, out var c) ? VariantResolver.EffectiveMax(c, ctx) : s.PointsAwarded;
            projected += Math.Min(max, s.PointsAwarded + stakeByCriterion.GetValueOrDefault(s.CriterionId));
            targeted += s.PointsTargeted;
        }

        var progress = targeted == 0 ? 0m : Math.Round((decimal)current.TotalPoints / targeted, 4);
        return new ProgressReport(
            current.TotalPoints,
            ScoringEngine.RatingFor(tool, current.TotalPoints),
            projected,
            ScoringEngine.RatingFor(tool, projected),
            targeted,
            Math.Min(progress, 1m),
            threshold,
            targeted > 0 && progress >= threshold,
            open.Count,
            ticketList.Count - open.Count);
    }
}

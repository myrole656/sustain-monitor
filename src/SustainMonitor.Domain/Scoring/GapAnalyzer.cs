using SustainMonitor.Domain.Entities;

namespace SustainMonitor.Domain.Scoring;

public sealed record GapItem(int CriterionId, string CriterionCode, string CriterionName, string CategoryCode, int PointsAtStake);

/// <summary>Finds criteria below target; each gap becomes a draft CO-ticket.</summary>
public static class GapAnalyzer
{
    public static IReadOnlyList<GapItem> FindGaps(GbiTool tool, Assessment assessment)
    {
        var lookup = tool.Categories
            .SelectMany(cat => cat.Criteria.Select(c => (cat, c)))
            .ToDictionary(x => x.c.Id);

        return assessment.CriterionScores
            .Where(s => s.Status != CriterionStatus.NotApplicable && s.PointsAwarded < s.PointsTargeted)
            .Select(s =>
            {
                var (cat, c) = lookup[s.CriterionId];
                return new GapItem(c.Id, c.Code, c.Name, cat.Code, s.PointsTargeted - s.PointsAwarded);
            })
            .OrderByDescending(g => g.PointsAtStake)
            .ThenBy(g => g.CriterionCode, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Creates unsaved tickets for gaps that do not already have an open ticket.</summary>
    public static List<Ticket> DraftTickets(Project project, IEnumerable<GapItem> gaps,
        IEnumerable<int> criteriaWithOpenTickets, string reporterId)
    {
        var skip = criteriaWithOpenTickets.ToHashSet();
        var drafts = new List<Ticket>();
        foreach (var gap in gaps.Where(g => !skip.Contains(g.CriterionId)))
        {
            drafts.Add(new Ticket
            {
                ProjectId = project.Id,
                Key = Ticket.FormatKey(project.Code, project.NextTicketNumber++),
                Title = $"{gap.CriterionCode} {gap.CriterionName}: close {gap.PointsAtStake}-point gap",
                CriterionId = gap.CriterionId,
                Type = TicketType.Gap,
                Priority = gap.PointsAtStake >= 5 ? TicketPriority.High
                         : gap.PointsAtStake >= 2 ? TicketPriority.Medium
                         : TicketPriority.Low,
                ReporterId = reporterId,
                PointsAtStake = gap.PointsAtStake,
            });
        }
        return drafts;
    }
}

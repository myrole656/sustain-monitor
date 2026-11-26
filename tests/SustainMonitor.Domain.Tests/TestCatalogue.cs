using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Scoring;

namespace SustainMonitor.Domain.Tests;

/// <summary>Gives in-memory catalogue rows ids, the way the database would.</summary>
internal static class TestCatalogue
{
    public static GbiTool WithIds(GbiTool tool)
    {
        tool.Id = 1;
        var catId = 1;
        var critId = 1;
        foreach (var cat in tool.Categories)
        {
            cat.Id = catId++;
            cat.ToolId = tool.Id;
            foreach (var c in cat.Criteria)
            {
                c.Id = critId++;
                c.CategoryId = cat.Id;
            }
        }
        return tool;
    }

    public static Criterion Find(GbiTool tool, string code) =>
        tool.Categories.SelectMany(c => c.Criteria).Single(c => c.Code == code);

    /// <summary>
    /// Every criterion targeted at its max; points awarded in catalogue order until <paramref name="total"/>
    /// is reached. Criteria in <paramref name="leaveAtZero"/> get 0 awarded.
    /// </summary>
    public static List<ScoreInput> ScoresTotalling(GbiTool tool, int total, params string[] leaveAtZero)
    {
        var remaining = total;
        var inputs = new List<ScoreInput>();
        foreach (var c in tool.Categories.OrderBy(x => x.SortOrder).SelectMany(x => x.Criteria.OrderBy(y => y.SortOrder)))
        {
            var awarded = leaveAtZero.Contains(c.Code) ? 0 : Math.Min(c.MaxPoints, remaining);
            remaining -= awarded;
            var status = awarded == c.MaxPoints ? CriterionStatus.Achieved
                       : awarded > 0 ? CriterionStatus.InProgress
                       : CriterionStatus.NotStarted;
            inputs.Add(new ScoreInput(c.Id, status, c.MaxPoints, awarded));
        }
        if (remaining != 0) throw new InvalidOperationException($"Could not reach {total}.");
        return inputs;
    }
}

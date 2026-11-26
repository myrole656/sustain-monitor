using SustainMonitor.Domain.Catalogue;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Scoring;
using Xunit;

namespace SustainMonitor.Domain.Tests;

public class ScoringEngineTests
{
    private static readonly ProjectContext Ctx = new(1000m);

    [Theory]
    [InlineData(100, "Platinum")]
    [InlineData(86, "Platinum")]
    [InlineData(85, "Gold")]
    [InlineData(76, "Gold")]
    [InlineData(75, "Silver")]
    [InlineData(66, "Silver")]
    [InlineData(65, "Certified")]
    [InlineData(50, "Certified")]
    [InlineData(49, "Not rated")]
    [InlineData(0, "Not rated")]
    public void Rating_bands_follow_GBI(int points, string expected)
    {
        Assert.Equal(expected, ScoringEngine.RatingFor(GbiCatalogue.Nreb(), points));
    }

    [Fact]
    public void Evaluate_sums_categories_and_rates()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var result = ScoringEngine.Evaluate(tool, TestCatalogue.ScoresTotalling(tool, 78), Ctx);

        Assert.Equal(78, result.TotalPoints);
        Assert.Equal(100, result.MaxPoints);
        Assert.Equal(RatingNames.Gold, result.Rating);
        Assert.Equal(6, result.Categories.Count);
        Assert.Equal(78, result.Categories.Sum(c => c.Points));
        Assert.Equal(44, result.Criteria.Count);
    }

    [Fact]
    public void Missing_criteria_count_as_not_started()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var ee5 = TestCatalogue.Find(tool, "EE5");
        var result = ScoringEngine.Evaluate(tool, [new ScoreInput(ee5.Id, CriterionStatus.Achieved, 15, 15)], Ctx);

        Assert.Equal(15, result.TotalPoints);
        Assert.Equal(43, result.Criteria.Count(c => c.Status == CriterionStatus.NotStarted));
    }

    [Fact]
    public void Not_applicable_scores_zero()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var we1 = TestCatalogue.Find(tool, "WE1");
        var result = ScoringEngine.Evaluate(tool, [new ScoreInput(we1.Id, CriterionStatus.NotApplicable, 0, 3)], Ctx);
        Assert.Equal(0, result.TotalPoints);
    }

    [Fact]
    public void Awarding_more_than_max_is_rejected()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var we1 = TestCatalogue.Find(tool, "WE1");
        var ex = Assert.Throws<ScoringException>(() =>
            ScoringEngine.Evaluate(tool, [new ScoreInput(we1.Id, CriterionStatus.Achieved, 3, 4)], Ctx));
        Assert.Contains("WE1", ex.Message);
    }

    [Fact]
    public void Variant_max_is_enforced()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Interiors());
        var ee2 = TestCatalogue.Find(tool, "EE2");
        Assert.Throws<ScoringException>(() =>
            ScoringEngine.Evaluate(tool, [new ScoreInput(ee2.Id, CriterionStatus.Achieved, 6, 12)], new ProjectContext(400m)));
    }

    [Fact]
    public void Unknown_and_duplicate_criteria_are_rejected()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        Assert.Throws<ScoringException>(() =>
            ScoringEngine.Evaluate(tool, [new ScoreInput(999, CriterionStatus.Achieved, 1, 1)], Ctx));

        var ee1 = TestCatalogue.Find(tool, "EE1");
        Assert.Throws<ScoringException>(() => ScoringEngine.Evaluate(tool,
            [new ScoreInput(ee1.Id, CriterionStatus.Achieved, 2, 2), new ScoreInput(ee1.Id, CriterionStatus.Achieved, 2, 1)], Ctx));
    }

    [Fact]
    public void Closing_a_ticket_rescores_into_a_new_snapshot()
    {
        // The plan's example: 74 points (Silver), close the WE1 ticket for 3 points → 77 (Gold).
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var we1 = TestCatalogue.Find(tool, "WE1");
        var inputs = TestCatalogue.ScoresTotalling(tool, 74, "WE1");
        var v1 = ScoringEngine.ToAssessment(ScoringEngine.Evaluate(tool, inputs, Ctx),
            projectId: 7, AssessmentStage.SelfCheck, versionNo: 1, AssessmentTrigger.Initial, "owner");
        Assert.Equal(74, v1.TotalPoints);
        Assert.Equal(RatingNames.Silver, v1.Rating);

        var v2 = ScoringEngine.Rescore(v1, tool, Ctx, we1.Id, newPointsAwarded: 3, ticketId: 12, closedBy: "reviewer");

        Assert.Equal(77, v2.TotalPoints);
        Assert.Equal(RatingNames.Gold, v2.Rating);
        Assert.Equal(2, v2.VersionNo);
        Assert.Equal(AssessmentTrigger.TicketClosed, v2.Trigger);
        Assert.Equal(12, v2.TriggerTicketId);
        Assert.True(v2.IsCurrent);
        Assert.False(v1.IsCurrent);
        Assert.Equal(CriterionStatus.Achieved, v2.CriterionScores.Single(s => s.CriterionId == we1.Id).Status);
        // v1 is untouched apart from IsCurrent: history is kept.
        Assert.Equal(0, v1.CriterionScores.Single(s => s.CriterionId == we1.Id).PointsAwarded);
    }

    [Fact]
    public void Rescore_of_old_snapshot_is_rejected()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var v1 = ScoringEngine.ToAssessment(ScoringEngine.Evaluate(tool, [], Ctx), 1,
            AssessmentStage.SelfCheck, 1, AssessmentTrigger.Initial, "owner");
        v1.IsCurrent = false;
        Assert.Throws<ScoringException>(() =>
            ScoringEngine.Rescore(v1, tool, Ctx, TestCatalogue.Find(tool, "EE1").Id, 2, 1, "r"));
    }
}

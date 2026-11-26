using SustainMonitor.Domain.Catalogue;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Scoring;
using SustainMonitor.Domain.Tickets;
using Xunit;

namespace SustainMonitor.Domain.Tests;

public class TicketTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 13, 0, 0, DateTimeKind.Utc);
    private const string Pm = "pm-1";
    private const string Sup = "sup-1";

    [Fact]
    public void Gaps_are_sorted_by_points_at_stake()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var v1 = Baseline(tool, 74, "WE1");

        var gaps = GapAnalyzer.FindGaps(tool, v1);

        Assert.Equal(26, gaps.Sum(g => g.PointsAtStake));
        Assert.True(gaps.Zip(gaps.Skip(1)).All(p => p.First.PointsAtStake >= p.Second.PointsAtStake));
        Assert.Contains(gaps, g => g.CriterionCode == "WE1" && g.PointsAtStake == 3);
    }

    [Fact]
    public void PM_creates_ticket_with_project_key_and_capped_stake()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var project = NewProject();
        var v1 = Baseline(tool, 74, "WE1");
        var we1 = TestCatalogue.Find(tool, "WE1");

        var t = TicketFactory.Create(project, tool, v1, we1.Id, "Install rainwater tank", null, 3,
            TicketType.Gap, TicketPriority.High, null, Pm, [], Now);

        Assert.Equal("KLT-CO-0011", t.Key);
        Assert.Equal(Pm, t.ReporterId);
        Assert.Equal(12, project.NextTicketNumber);

        // WE1 max is 3: nothing left to claim while that ticket is open.
        var ex = Assert.Throws<TicketWorkflowException>(() => TicketFactory.Create(project, tool, v1, we1.Id, "Again", null, 1,
            TicketType.Gap, TicketPriority.Low, null, Pm, [t], Now));
        Assert.Contains("no points left", ex.Message);

        Assert.Throws<TicketWorkflowException>(() => TicketFactory.Create(project, tool, v1, we1.Id, "Too much", null, 4,
            TicketType.Gap, TicketPriority.Low, null, Pm, [], Now));
    }

    [Fact]
    public void Only_PM_moves_work_and_needs_evidence_for_review()
    {
        var t = NewTicket();
        Assert.Throws<TicketWorkflowException>(() => TicketWorkflow.Move(t, TicketStatus.InProgress, "ceo", CompanyRole.CEO, Now, 0));
        Assert.Throws<TicketWorkflowException>(() => TicketWorkflow.Move(t, TicketStatus.InProgress, Sup, CompanyRole.Superior, Now, 0));

        TicketWorkflow.Move(t, TicketStatus.InProgress, Pm, CompanyRole.ProjectManager, Now, 0);
        var ex = Assert.Throws<TicketWorkflowException>(() => TicketWorkflow.Move(t, TicketStatus.InReview, Pm, CompanyRole.ProjectManager, Now, 0));
        Assert.Contains("evidence", ex.Message);

        TicketWorkflow.Move(t, TicketStatus.InReview, Pm, CompanyRole.ProjectManager, Now, evidenceCount: 2);
        Assert.Equal(TicketStatus.InReview, t.Status);
        Assert.Equal(2, t.History.Count);
    }

    [Fact]
    public void Only_Superior_approves_and_sets_points_within_stake()
    {
        var t = InReview();
        Assert.Throws<TicketWorkflowException>(() => TicketWorkflow.Move(t, TicketStatus.Done, Pm, CompanyRole.ProjectManager, Now, 1, 3));
        Assert.Throws<TicketWorkflowException>(() => TicketWorkflow.Move(t, TicketStatus.Done, Sup, CompanyRole.Superior, Now, 1));
        Assert.Throws<TicketWorkflowException>(() => TicketWorkflow.Move(t, TicketStatus.Done, Sup, CompanyRole.Superior, Now, 1, 4));

        TicketWorkflow.Move(t, TicketStatus.Done, Sup, CompanyRole.Superior, Now, 1, pointsGained: 3);
        Assert.Equal(TicketStatus.Done, t.Status);
        Assert.Equal(3, t.PointsAwardedOnClose);
        Assert.Equal(Sup, t.ReviewerId);
        Assert.Equal(Now, t.ClosedAt);
    }

    [Fact]
    public void Superior_who_raised_the_ticket_cannot_approve_it()
    {
        var t = InReview();
        t.ReporterId = Sup;
        Assert.Throws<TicketWorkflowException>(() => TicketWorkflow.Move(t, TicketStatus.Done, Sup, CompanyRole.Superior, Now, 1, 3));
    }

    [Fact]
    public void Superior_can_reject_back_to_in_progress()
    {
        var t = InReview();
        TicketWorkflow.Move(t, TicketStatus.InProgress, Sup, CompanyRole.Superior, Now, 1, note: "Bills missing");
        Assert.Equal(TicketStatus.InProgress, t.Status);
        Assert.Null(t.ClosedAt);
        Assert.Equal("InProgress: Bills missing", t.History[^1].NewValue);
    }

    [Fact]
    public void Board_buttons_depend_on_role()
    {
        Assert.Equal(TicketStatus.InProgress, Assert.Single(TicketWorkflow.NextFor(TicketStatus.Open, CompanyRole.ProjectManager)));
        Assert.Empty(TicketWorkflow.NextFor(TicketStatus.Open, CompanyRole.CEO));
        Assert.Equal(2, TicketWorkflow.NextFor(TicketStatus.InReview, CompanyRole.Superior).Count);
        Assert.Empty(TicketWorkflow.NextFor(TicketStatus.InReview, CompanyRole.ProjectManager));
    }

    [Fact]
    public void Approved_ticket_adds_points_and_rescores()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var project = NewProject();
        var v1 = Baseline(tool, 74, "WE1");
        var we1 = TestCatalogue.Find(tool, "WE1");
        var t = TicketFactory.Create(project, tool, v1, we1.Id, "Rainwater tank", null, 3, TicketType.Gap, TicketPriority.High, null, Pm, [], Now);
        t.Id = 5;
        TicketWorkflow.Move(t, TicketStatus.InProgress, Pm, CompanyRole.ProjectManager, Now, 0);
        TicketWorkflow.Move(t, TicketStatus.InReview, Pm, CompanyRole.ProjectManager, Now, 1);
        TicketWorkflow.Move(t, TicketStatus.Done, Sup, CompanyRole.Superior, Now, 1, 3);

        var v2 = TicketFactory.ApplyApproved(t, v1, tool, ProjectContext.From(project));

        Assert.Equal(77, v2.TotalPoints);
        Assert.Equal(RatingNames.Gold, v2.Rating);
        Assert.Equal(5, v2.TriggerTicketId);
        Assert.Equal(Sup, v2.CreatedBy);
    }

    internal static Assessment Baseline(GbiTool tool, int total, params string[] zero) =>
        ScoringEngine.ToAssessment(
            ScoringEngine.Evaluate(tool, TestCatalogue.ScoresTotalling(tool, total, zero), new ProjectContext(null)),
            1, AssessmentStage.SelfCheck, 1, AssessmentTrigger.Initial, Pm);

    internal static Project NewProject() => new()
    {
        Id = 1, CompanyId = 1, Code = "KLT", Name = "Kelulut HQ", CreatedById = Pm, NextTicketNumber = 11,
    };

    private static Ticket NewTicket() => new()
    {
        Id = 1, ProjectId = 1, Key = "KLT-CO-0001", Title = "WE1", CriterionId = 1,
        ReporterId = Pm, AssigneeId = Pm, PointsAtStake = 3,
    };

    private static Ticket InReview()
    {
        var t = NewTicket();
        TicketWorkflow.Move(t, TicketStatus.InProgress, Pm, CompanyRole.ProjectManager, Now, 0);
        TicketWorkflow.Move(t, TicketStatus.InReview, Pm, CompanyRole.ProjectManager, Now, 1);
        return t;
    }
}

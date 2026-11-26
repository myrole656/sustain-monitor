using SustainMonitor.Domain.Catalogue;
using SustainMonitor.Domain.Certificates;
using SustainMonitor.Domain.Companies;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Scoring;
using SustainMonitor.Domain.Security;
using Xunit;

namespace SustainMonitor.Domain.Tests;

public class ProgressAndCertificateTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 13, 0, 0, DateTimeKind.Utc);

    private static Ticket OpenTicket(int criterionId, int stake, TicketStatus status = TicketStatus.Open) => new()
    {
        Key = "KLT-CO-0001", Title = "t", CriterionId = criterionId, ReporterId = "pm", PointsAtStake = stake, Status = status,
    };

    [Fact]
    public void Projected_score_adds_open_ticket_points()
    {
        // The worked example: baseline 62 (Certified), tickets EE5 +8, WE1 +3, EQ8 +2 → projected 75 (Silver).
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var inputs = TestCatalogue.ScoresTotalling(tool, 62, "EE5", "WE1", "EQ8");
        var v1 = ScoringEngine.ToAssessment(ScoringEngine.Evaluate(tool, inputs, new ProjectContext(null)), 1,
            AssessmentStage.SelfCheck, 1, AssessmentTrigger.Initial, "pm");
        Ticket[] tickets =
        [
            OpenTicket(TestCatalogue.Find(tool, "EE5").Id, 8),
            OpenTicket(TestCatalogue.Find(tool, "WE1").Id, 3, TicketStatus.InReview),
            OpenTicket(TestCatalogue.Find(tool, "EQ8").Id, 2),
        ];

        var r = ProgressCalculator.Calculate(tool, new ProjectContext(null), v1, tickets);

        Assert.Equal(62, r.ConfirmedPoints);
        Assert.Equal(RatingNames.Certified, r.ConfirmedRating);
        Assert.Equal(75, r.ProjectedPoints);
        Assert.Equal(RatingNames.Silver, r.ProjectedRating);
        Assert.Equal(3, r.OpenTickets);
    }

    [Fact]
    public void Projected_score_never_exceeds_criterion_max()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var we1 = TestCatalogue.Find(tool, "WE1");
        var v1 = ScoringEngine.ToAssessment(ScoringEngine.Evaluate(tool,
            [new ScoreInput(we1.Id, CriterionStatus.InProgress, 3, 2)], new ProjectContext(null)), 1,
            AssessmentStage.SelfCheck, 1, AssessmentTrigger.Initial, "pm");

        var r = ProgressCalculator.Calculate(tool, new ProjectContext(null), v1, [OpenTicket(we1.Id, 3)]);
        Assert.Equal(3, r.ProjectedPoints);
    }

    [Theory]
    [InlineData(79, false)]
    [InlineData(80, true)]
    [InlineData(100, true)]
    public void Analysis_unlocks_at_80_percent_of_target(int confirmed, bool expected)
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        // Every criterion targeted at max (100 points), so progress = confirmed / 100.
        var v1 = ScoringEngine.ToAssessment(ScoringEngine.Evaluate(tool, TestCatalogue.ScoresTotalling(tool, confirmed),
            new ProjectContext(null)), 1, AssessmentStage.SelfCheck, 1, AssessmentTrigger.Initial, "pm");

        var r = ProgressCalculator.Calculate(tool, new ProjectContext(null), v1, []);

        Assert.Equal(expected, r.CanRunAnalysis);
        Assert.Equal(confirmed / 100m, r.Progress);
    }

    [Fact]
    public void Analysis_and_certificate_permissions_follow_roles()
    {
        Assert.True(Permissions.CanRunAnalysis(CompanyRole.CEO));
        Assert.True(Permissions.CanRunAnalysis(CompanyRole.ProjectManager));
        Assert.False(Permissions.CanRunAnalysis(CompanyRole.Superior));
        Assert.True(Permissions.CanCreateTicket(CompanyRole.ProjectManager));
        Assert.False(Permissions.CanCreateTicket(CompanyRole.CEO));
        Assert.True(Permissions.CanReviewTicket(CompanyRole.Superior));
        Assert.False(Permissions.CanReviewTicket(CompanyRole.CEO));
        Assert.False(Permissions.CanDownloadCertificate(CompanyRole.Superior));
    }

    [Fact]
    public void Ecert_is_requested_only_at_50_points_or_more()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Nreb());
        var project = TicketTests.NewProject();
        var low = TicketTests.Baseline(tool, 49);
        var ok = TicketTests.Baseline(tool, 50);

        Assert.Null(ECertificatePolicy.Request(tool, project, low, "ceo", Now, []));
        var req = ECertificatePolicy.Request(tool, project, ok, "ceo", Now, []);
        Assert.NotNull(req);
        Assert.Equal(ECertStatus.Requested, req!.Status);
        Assert.Equal(RatingNames.Certified, req.Rating);
        Assert.Equal("NREB", req.ToolCode);

        Assert.Throws<ECertificateException>(() => ECertificatePolicy.Request(tool, project, ok, "ceo", Now, [req]));
    }

    [Fact]
    public void Admin_approval_issues_three_year_certificate()
    {
        var cert = Requested();
        ECertificatePolicy.Approve(cert, "admin", 42, "abcdef0123456789abcdef", Now);

        Assert.Equal(ECertStatus.Issued, cert.Status);
        Assert.Equal("SM-2026-000042", cert.CertNo);
        Assert.Equal(Now.AddYears(3), cert.ExpiresAt);
        Assert.Equal("Valid", ECertificatePolicy.PublicStatus(cert, Now));
        Assert.Equal("Expired", ECertificatePolicy.PublicStatus(cert, Now.AddYears(3)));

        Assert.Throws<ECertificateException>(() => ECertificatePolicy.Approve(cert, "admin", 43, "abcdef0123456789abcdef", Now));
        ECertificatePolicy.Revoke(cert, "admin", "Evidence found to be false", Now);
        Assert.Equal("Revoked", ECertificatePolicy.PublicStatus(cert, Now));
    }

    [Fact]
    public void Decline_needs_a_reason()
    {
        var cert = Requested();
        Assert.Throws<ECertificateException>(() => ECertificatePolicy.Decline(cert, "admin", " ", Now));
        ECertificatePolicy.Decline(cert, "admin", "EE5 bills incomplete", Now);
        Assert.Equal(ECertStatus.Declined, cert.Status);
        Assert.Equal("Not issued", ECertificatePolicy.PublicStatus(cert, Now));
    }

    [Fact]
    public void Company_registration_is_validated_and_verified_by_admin()
    {
        Assert.Empty(CompanyRules.ValidateRegistration("Binaan Hijau Sdn Bhd", "202301012345", "G5"));
        Assert.Empty(CompanyRules.ValidateRegistration("Old Co", "123456-a", "g7"));
        Assert.Equal(2, CompanyRules.ValidateRegistration("X", "12345", "G9").Count);

        var c = new Company { Name = "Binaan Hijau", SsmNo = "202301012345", CidbGrade = "G5" };
        Assert.Throws<CompanyException>(() => CompanyRules.EnsureActive(c));
        CompanyRules.Verify(c, "admin", Now);
        CompanyRules.EnsureActive(c);
        Assert.Equal(CompanyStatus.Verified, c.Status);
    }

    [Fact]
    public void Suspended_company_is_blocked_until_reverified()
    {
        var c = new Company { Name = "Binaan Hijau", SsmNo = "202301012345", CidbGrade = "G5" };
        Assert.Throws<CompanyException>(() => CompanyRules.Suspend(c, "admin", "Fake evidence", Now)); // still pending
        CompanyRules.Verify(c, "admin", Now);
        Assert.Throws<CompanyException>(() => CompanyRules.Suspend(c, "admin", "", Now));
        CompanyRules.Suspend(c, "admin", "Fake evidence", Now);
        Assert.Throws<CompanyException>(() => CompanyRules.EnsureActive(c));
        CompanyRules.Verify(c, "admin", Now);
        CompanyRules.EnsureActive(c);
        Assert.Null(c.StatusNote);
    }

    [Fact]
    public void Only_CEO_invites_and_never_a_second_CEO()
    {
        CompanyRules.EnsureCanInvite(CompanyRole.CEO, CompanyRole.ProjectManager);
        CompanyRules.EnsureCanInvite(CompanyRole.CEO, CompanyRole.Superior);
        Assert.Throws<CompanyException>(() => CompanyRules.EnsureCanInvite(CompanyRole.ProjectManager, CompanyRole.Superior));
        Assert.Throws<CompanyException>(() => CompanyRules.EnsureCanInvite(CompanyRole.CEO, CompanyRole.CEO));
    }

    private static ECertificate Requested() => new()
    {
        ProjectId = 1, AssessmentId = 1, Points = 77, Rating = RatingNames.Gold, ToolCode = "NREB", ToolVersion = "1.1",
        RequestedBy = "ceo", RequestedAt = Now,
    };
}

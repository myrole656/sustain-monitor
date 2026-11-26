using System.Net;

namespace SustainMonitor.Api.Tests;

/// <summary>GBI rules enforced at the API boundary.</summary>
public class BusinessRuleTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Baseline_can_only_be_submitted_once()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new { stage = "SelfCheck", scores = CompanyTeam.Baseline47(c) })).Json200();
        var again = await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new { stage = "SelfCheck", scores = CompanyTeam.Baseline47(c) });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Baseline_cannot_award_more_than_the_GBI_maximum()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        var res = await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new
        {
            stage = "SelfCheck",
            scores = new[] { new { criterionId = c["WE1"], status = "Achieved", pointsTargeted = 3, pointsAwarded = 4 } },
        });
        Assert.Contains("WE1", await res.Fails(HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Tickets_cannot_claim_more_points_than_are_left()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new { stage = "SelfCheck", scores = CompanyTeam.Baseline47(c) })).Json200();

        // EE5: max 15, awarded 10 → 5 left.
        var tooMany = await team.Pm.PostJson($"/api/projects/{projectId}/tickets", new { criterionId = c["EE5"], title = "Chiller", pointsAtStake = 6 });
        await tooMany.Fails(HttpStatusCode.BadRequest);
        await (await team.Pm.PostJson($"/api/projects/{projectId}/tickets", new { criterionId = c["EE5"], title = "Chiller", pointsAtStake = 5 })).Json200();
        var nothingLeft = await team.Pm.PostJson($"/api/projects/{projectId}/tickets", new { criterionId = c["EE5"], title = "Lighting", pointsAtStake = 1 });
        Assert.Contains("no points left", await nothingLeft.Fails(HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Tickets_need_a_baseline_first()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        var res = await team.Pm.PostJson($"/api/projects/{projectId}/tickets", new { criterionId = c["WE1"], title = "x", pointsAtStake = 1 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Analysis_stays_locked_below_80_percent()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        // 10 of 30 targeted = 33%.
        await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new
        {
            stage = "SelfCheck",
            scores = new object[]
            {
                new { criterionId = c["EE5"], status = "InProgress", pointsTargeted = 15, pointsAwarded = 10 },
                new { criterionId = c["EQ15"], status = "NotStarted", pointsTargeted = 2, pointsAwarded = 0 },
                new { criterionId = c["EE4"], status = "NotStarted", pointsTargeted = 5, pointsAwarded = 0 },
                new { criterionId = c["WE1"], status = "NotStarted", pointsTargeted = 3, pointsAwarded = 0 },
                new { criterionId = c["IN1"], status = "NotStarted", pointsTargeted = 5, pointsAwarded = 0 },
            },
        })).Json200();

        var detail = await team.Ceo.GetJson($"/api/projects/{projectId}");
        Assert.False(detail.GetProperty("progress").GetProperty("canRunAnalysis").GetBoolean());
        var res = await team.Ceo.PostJson($"/api/projects/{projectId}/analysis");
        Assert.Contains("80%", await res.Fails(HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Analysis_below_50_points_gives_no_ecert()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        // 10 of 10 targeted = 100%, but only 10 points: no e-cert.
        await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new
        {
            stage = "SelfCheck",
            scores = new[] { new { criterionId = c["EE5"], status = "Achieved", pointsTargeted = 10, pointsAwarded = 10 } },
        })).Json200();

        var analysis = await (await team.Pm.PostJson($"/api/projects/{projectId}/analysis")).Json200();
        Assert.Equal("NotEligible", analysis.GetProperty("certificate").Str("status"));
    }

    [Fact]
    public async Task Interiors_EE2_max_depends_on_floor_area()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var tools = await team.Pm.GetJson("/api/tools");
        var id = tools.EnumerateArray().Single(t => t.Str("code") == "ID").Int("id");
        var tool = await team.Pm.GetJson($"/api/tools/{id}");
        var ee2 = tool.GetProperty("categories").EnumerateArray().SelectMany(c => c.GetProperty("criteria").EnumerateArray()).Single(c => c.Str("code") == "EE2");

        var big = await team.Pm.PostJson($"/api/tools/{id}/score-preview", new
        {
            gfaM2 = 500,
            scores = new[] { new { criterionId = ee2.Int("id"), status = "Achieved", pointsTargeted = 6, pointsAwarded = 12 } },
        });
        await big.Fails(HttpStatusCode.BadRequest);

        var small = await (await team.Pm.PostJson($"/api/tools/{id}/score-preview", new
        {
            gfaM2 = 250,
            scores = new[] { new { criterionId = ee2.Int("id"), status = "Achieved", pointsTargeted = 12, pointsAwarded = 12 } },
        })).Json200();
        Assert.Equal(12, small.Int("totalPoints"));
    }

    [Fact]
    public async Task Ecert_decisions_need_reasons_and_can_be_revoked()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        var scores = CompanyTeam.Baseline47(c).ToList();
        scores.Add(new { criterionId = c["WE2"], status = "Achieved", pointsTargeted = 2, pointsAwarded = 2 });
        scores.Add(new { criterionId = c["WE5"], status = "Achieved", pointsTargeted = 2, pointsAwarded = 2 }); // 51 of 59 = 86%
        await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new { stage = "SelfCheck", scores })).Json200();
        await (await team.Ceo.PostJson($"/api/projects/{projectId}/analysis")).Json200();

        var admin = await f.AdminAsync();
        var certs = await team.Ceo.GetJson($"/api/projects/{projectId}/ecerts");
        var certId = certs[0].Int("id");

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostJson($"/api/admin/ecerts/{certId}/decline", new { reason = "" })).StatusCode);
        await (await admin.PostJson($"/api/admin/ecerts/{certId}/approve")).Json200();
        var issued = await team.Ceo.GetJson($"/api/projects/{projectId}/ecerts");
        var verifyUrl = issued[0].Str("verifyUrl");

        await (await admin.PostJson($"/api/admin/ecerts/{certId}/revoke", new { reason = "Evidence withdrawn" })).Json200();
        var verify = await f.Anonymous().GetJson(verifyUrl);
        Assert.Equal("Revoked", verify.Str("status"));
    }
}

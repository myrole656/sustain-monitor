using System.Net;
using System.Net.Http.Headers;

namespace SustainMonitor.Api.Tests;

/// <summary>The whole business flow through HTTP, exactly as the website drives it.</summary>
public class FullFlowTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Company_to_ecert_end_to_end()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();

        // Baseline: 47 points, Not rated, and the gaps are suggested as tickets.
        var baseline = await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline",
            new { stage = "SelfCheck", scores = CompanyTeam.Baseline47(c) })).Json200();
        Assert.Equal(47, baseline.Int("totalPoints"));
        Assert.Equal("Not rated", baseline.Str("rating"));
        Assert.Contains(baseline.GetProperty("suggestedTickets").EnumerateArray(), s => s.Str("criterionCode") == "WE1" && s.Int("pointsAtStake") == 3);

        // PM raises a CO-ticket for WE1.
        var ticket = await (await team.Pm.PostJson($"/api/projects/{projectId}/tickets",
            new { criterionId = c["WE1"], title = "Install rainwater harvesting tank", pointsAtStake = 3, priority = "High" })).Json200();
        var ticketId = ticket.Int("id");
        Assert.EndsWith("-CO-0001", ticket.Str("key"));

        // Projected score includes the open ticket.
        var detail = await team.Ceo.GetJson($"/api/projects/{projectId}");
        Assert.Equal(47, detail.GetProperty("progress").Int("confirmedPoints"));
        Assert.Equal(50, detail.GetProperty("progress").Int("projectedPoints"));
        Assert.Equal("Certified", detail.GetProperty("progress").Str("projectedRating"));

        // Work it: start, try review without evidence (blocked), upload, send for review.
        await (await team.Pm.PostJson($"/api/tickets/{ticketId}/move", new { to = "InProgress" })).Json200();
        var noEvidence = await (await team.Pm.PostJson($"/api/tickets/{ticketId}/move", new { to = "InReview" })).Fails(HttpStatusCode.BadRequest);
        Assert.Contains("evidence", noEvidence, StringComparison.OrdinalIgnoreCase);

        using (var form = new MultipartFormDataContent())
        {
            var file = new ByteArrayContent("%PDF-1.4 test evidence"u8.ToArray());
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(file, "file", "tank-invoice.pdf");
            await (await team.Pm.PostAsync($"/api/tickets/{ticketId}/evidence?stage=CompletionVerification", form)).Json200();
        }
        await (await team.Pm.PostJson($"/api/tickets/{ticketId}/move", new { to = "InReview" })).Json200();

        // The PM cannot approve; the Superior can, and the project is re-scored to 50 (Certified).
        await (await team.Pm.PostJson($"/api/tickets/{ticketId}/move", new { to = "Done", pointsGained = 3 })).Fails(HttpStatusCode.BadRequest);
        var approved = await (await team.Sup.PostJson($"/api/tickets/{ticketId}/move", new { to = "Done", pointsGained = 3 })).Json200();
        var score = approved.GetProperty("score");
        Assert.Equal(47, score.GetProperty("before").Int("totalPoints"));
        Assert.Equal(50, score.GetProperty("after").Int("totalPoints"));
        Assert.Equal("Certified", score.GetProperty("after").Str("rating"));
        Assert.True(score.GetProperty("ratingChanged").GetBoolean());

        // History keeps both snapshots.
        var history = await team.Ceo.GetJson($"/api/projects/{projectId}/history");
        Assert.Equal([47, 50], history.EnumerateArray().Select(h => h.Int("totalPoints")).ToArray());

        // Evidence can be downloaded by the company.
        var t = await team.Sup.GetJson($"/api/tickets/{ticketId}");
        var evidenceId = t.GetProperty("evidence")[0].Int("id");
        var download = await team.Ceo.GetAsync($"/api/tickets/{ticketId}/evidence/{evidenceId}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.StartsWith("%PDF", await download.Content.ReadAsStringAsync());

        // 50 of 55 targeted = 91% ≥ 80%: the CEO runs the analysis, which requests the e-cert.
        var analysis = await (await team.Ceo.PostJson($"/api/projects/{projectId}/analysis")).Json200();
        Assert.Equal(50, analysis.Int("totalPoints"));
        Assert.Equal("Requested", analysis.GetProperty("certificate").Str("status"));

        // Running again does not create a second request.
        var again = await (await team.Ceo.PostJson($"/api/projects/{projectId}/analysis")).Json200();
        Assert.Equal("AlreadyRequested", again.GetProperty("certificate").Str("status"));

        // Platform admin approves; the public page confirms it.
        var admin = await f.AdminAsync();
        var requests = await admin.GetJson("/api/admin/ecerts?status=Requested");
        var certId = requests.EnumerateArray().Single(r => r.Int("points") == 50).Int("id");
        var issued = await (await admin.PostJson($"/api/admin/ecerts/{certId}/approve")).Json200();
        Assert.Matches(@"^SM-\d{4}-\d{6}$", issued.Str("certNo"));

        var verify = await f.Anonymous().GetJson(issued.Str("verifyUrl"));
        Assert.Equal("Valid", verify.Str("status"));
        Assert.Equal(50, verify.Int("points"));
        Assert.Contains("not an official", verify.Str("disclaimer"));

        // The CEO can open the printable certificate.
        var print = await team.Ceo.GetJson($"/api/ecerts/{certId}");
        Assert.Equal(issued.Str("certNo"), print.Str("certNo"));
        Assert.Equal(6, print.GetProperty("categories").GetArrayLength());

        // People were told along the way.
        var pmNotes = await team.Pm.GetJson("/api/notifications");
        Assert.Contains(pmNotes.EnumerateArray(), n => n.Str("type") == "ticket.approved");
        var ceoNotes = await team.Ceo.GetJson("/api/notifications");
        Assert.Contains(ceoNotes.EnumerateArray(), n => n.Str("type") == "ecert.issued");
    }

    [Fact]
    public async Task Rejected_ticket_goes_back_to_the_pm_with_the_note()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var (projectId, c) = await team.CreateProjectAsync();
        await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new { stage = "SelfCheck", scores = CompanyTeam.Baseline47(c) })).Json200();
        var id = (await (await team.Pm.PostJson($"/api/projects/{projectId}/tickets", new { criterionId = c["WE1"], title = "Tank", pointsAtStake = 3 })).Json200()).Int("id");
        await (await team.Pm.PostJson($"/api/tickets/{id}/move", new { to = "InProgress" })).Json200();
        using (var form = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "file", "photo.png" } })
            await (await team.Pm.PostAsync($"/api/tickets/{id}/evidence", form)).Json200();
        await (await team.Pm.PostJson($"/api/tickets/{id}/move", new { to = "InReview" })).Json200();

        await (await team.Sup.PostJson($"/api/tickets/{id}/move", new { to = "InProgress", note = "Bills missing" })).Json200();

        var t = await team.Pm.GetJson($"/api/tickets/{id}");
        Assert.Equal("InProgress", t.Str("status"));
        Assert.Contains(t.GetProperty("history").EnumerateArray(), h => h.Str("newValue").Contains("Bills missing"));
        var detail = await team.Pm.GetJson($"/api/projects/{projectId}");
        Assert.Equal(47, detail.GetProperty("current").Int("totalPoints")); // score unchanged
        var notes = await team.Pm.GetJson("/api/notifications");
        Assert.Contains(notes.EnumerateArray(), n => n.Str("type") == "ticket.rejected");
    }
}

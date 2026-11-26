using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SustainMonitor.Api.Tests;

/// <summary>Sign-in, roles, company isolation and account locking.</summary>
public class SecurityTests(ApiFactory f) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Startup_seeds_tools_roles_and_admin()
    {
        Assert.True(File.Exists(f.DatabasePath), "API must use the test database");
        var health = await f.Anonymous().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        var admin = await f.AdminAsync();
        var me = await admin.GetJson("/api/auth/me");
        Assert.Contains("PlatformAdmin", me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        var tools = await admin.GetJson("/api/tools");
        Assert.Equal(["ID", "NREB"], tools.EnumerateArray().Select(t => t.Str("code")).Order().ToArray());
    }

    [Fact]
    public async Task Api_requires_a_token()
    {
        var anon = f.Anonymous();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/projects")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/admin/companies")).StatusCode);
    }

    [Fact]
    public async Task Open_self_registration_is_switched_off()
    {
        var res = await f.Anonymous().PostJson("/api/auth/register", new { email = "x@test.local", password = ApiFactory.Password });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_rejected_and_locks_after_five_tries()
    {
        var email = CompanyTeam.Unique("lock") + "@test.local";
        await (await CompanyTeam.RegisterAsync(f, email)).Json200();
        var anon = f.Anonymous();
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostJson("/api/auth/login", new { email, password = "Wrong2026Pass" })).StatusCode);
        // Locked now, even with the right password.
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostJson("/api/auth/login", new { email, password = ApiFactory.Password })).StatusCode);
    }

    [Fact]
    public async Task Company_registration_is_validated()
    {
        var bad = await f.Anonymous().PostJson("/api/companies/register", new
        {
            companyName = "X", ssmNo = "123", cidbGrade = "G9", ceoName = "A", ceoEmail = "a@test.local", password = ApiFactory.Password,
        });
        var text = await bad.Fails(HttpStatusCode.BadRequest);
        Assert.Contains("SSM", text);
        Assert.Contains("CIDB", text);

        var ssm = CompanyTeam.Ssm();
        await (await CompanyTeam.RegisterAsync(f, CompanyTeam.Unique("a") + "@test.local", ssm)).Json200();
        var dup = await CompanyTeam.RegisterAsync(f, CompanyTeam.Unique("b") + "@test.local", ssm);
        Assert.Contains("already registered", await dup.Fails(HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Unverified_company_cannot_add_team()
    {
        var email = CompanyTeam.Unique("pending") + "@test.local";
        await (await CompanyTeam.RegisterAsync(f, email)).Json200();
        var ceo = await f.LoginAsync(email, ApiFactory.Password);

        var company = await ceo.GetJson("/api/company");
        Assert.Equal("Pending", company.Str("status"));
        var invite = await ceo.PostJson("/api/company/users", new { fullName = "PM", email = CompanyTeam.Unique("x") + "@test.local", role = "ProjectManager", temporaryPassword = "Temp2026Pass" });
        Assert.Contains("not verified", await invite.Fails(HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task Invited_user_must_change_temporary_password_first()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var email = CompanyTeam.Unique("newpm") + "@test.local";
        await (await team.Ceo.PostJson("/api/company/users", new { fullName = "New PM", email, role = "ProjectManager", temporaryPassword = "Temp2026Pass" })).Json200();

        var temp = await f.LoginAsync(email, "Temp2026Pass");
        Assert.Equal(HttpStatusCode.Forbidden, (await temp.GetAsync("/api/projects")).StatusCode);
        var me = await temp.GetJson("/api/auth/me");
        Assert.True(me.GetProperty("mustChangePassword").GetBoolean());

        await (await temp.PostJson("/api/auth/change-password", new { currentPassword = "Temp2026Pass", newPassword = ApiFactory.Password })).Json200();
        Assert.Equal(HttpStatusCode.OK, (await temp.GetAsync("/api/projects")).StatusCode);
    }

    [Fact]
    public async Task Each_role_only_does_its_own_job()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var tools = await team.Pm.GetJson("/api/tools");
        var toolId = tools[0].Int("id");

        // Only the PM creates projects.
        Assert.Equal(HttpStatusCode.Forbidden, (await team.Ceo.PostJson("/api/projects", new { code = "CEOX", name = "x", toolId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await team.Sup.PostJson("/api/projects", new { code = "SUPX", name = "x", toolId })).StatusCode);

        var (projectId, c) = await team.CreateProjectAsync();
        await (await team.Pm.PostJson($"/api/projects/{projectId}/baseline", new { stage = "SelfCheck", scores = CompanyTeam.Baseline47(c) })).Json200();

        // Only the PM raises tickets; only the CEO invites; only CEO/PM run analysis; company users can't use admin.
        Assert.Equal(HttpStatusCode.Forbidden, (await team.Ceo.PostJson($"/api/projects/{projectId}/tickets", new { criterionId = c["WE1"], title = "x", pointsAtStake = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await team.Sup.PostJson($"/api/projects/{projectId}/tickets", new { criterionId = c["WE1"], title = "x", pointsAtStake = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await team.Pm.PostJson("/api/company/users", new { fullName = "x", email = "x@test.local", role = "Superior", temporaryPassword = "Temp2026Pass" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await team.Sup.PostJson($"/api/projects/{projectId}/analysis")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await team.Ceo.GetAsync("/api/admin/companies")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await team.Sup.GetAsync($"/api/projects/{projectId}/ecerts")).StatusCode);
    }

    [Fact]
    public async Task Companies_cannot_see_each_others_projects()
    {
        var a = await CompanyTeam.CreateAsync(f);
        var b = await CompanyTeam.CreateAsync(f);
        var (projectId, _) = await a.CreateProjectAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await b.Ceo.GetAsync($"/api/projects/{projectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.Pm.GetAsync($"/api/projects/{projectId}/tickets")).StatusCode);
        var list = await b.Ceo.GetJson("/api/projects");
        Assert.DoesNotContain(list.EnumerateArray(), p => p.Int("id") == projectId);
    }

    [Fact]
    public async Task Deactivated_user_is_locked_out_immediately()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var company = await team.Ceo.GetJson("/api/company");
        var pmId = company.GetProperty("team").EnumerateArray().Single(m => m.Str("role") == "ProjectManager").Str("userId");

        await (await team.Ceo.PostJson($"/api/company/users/{pmId}/deactivate")).Json200();

        Assert.Equal(HttpStatusCode.Unauthorized, (await team.Pm.GetAsync("/api/projects")).StatusCode); // existing token
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Anonymous().PostJson("/api/auth/login", new { email = team.PmEmail, password = ApiFactory.Password })).StatusCode);

        await (await team.Ceo.PostJson($"/api/company/users/{pmId}/reactivate")).Json200();
        await f.LoginAsync(team.PmEmail, ApiFactory.Password);
    }

    [Fact]
    public async Task Suspending_a_company_locks_everyone_until_reverified()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var admin = await f.AdminAsync();

        await (await admin.PostJson($"/api/admin/companies/{team.CompanyId}/suspend", new { reason = "Investigation" })).Json200();
        Assert.Equal(HttpStatusCode.Unauthorized, (await team.Ceo.GetAsync("/api/company")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Anonymous().PostJson("/api/auth/login", new { email = team.SupEmail, password = ApiFactory.Password })).StatusCode);

        await (await admin.PostJson($"/api/admin/companies/{team.CompanyId}/verify")).Json200();
        var ceo = await f.LoginAsync(team.CeoEmail, ApiFactory.Password);
        Assert.Equal("Verified", (await ceo.GetJson("/api/company")).Str("status"));
    }

    [Fact]
    public async Task Refresh_token_issues_a_new_access_token()
    {
        var team = await CompanyTeam.CreateAsync(f);
        var anon = f.Anonymous();
        var login = await (await anon.PostJson("/api/auth/login", new { email = team.CeoEmail, password = ApiFactory.Password })).Json200();
        var refreshed = await (await anon.PostJson("/api/auth/refresh", new { refreshToken = login.Str("refreshToken") })).Json200();

        anon.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Str("accessToken"));
        Assert.Equal(HttpStatusCode.OK, (await anon.GetAsync("/api/company")).StatusCode);
    }

    [Fact]
    public async Task Unknown_public_certificate_is_not_found()
    {
        var res = await f.Anonymous().GetAsync("/api/verify/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var res = await f.Anonymous().GetAsync("/");
        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").Single());
    }
}

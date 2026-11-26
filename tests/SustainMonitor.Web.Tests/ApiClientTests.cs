using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using SustainMonitor.Domain;
using SustainMonitor.Web.Models;
using SustainMonitor.Web.Services;
using Xunit;

namespace SustainMonitor.Web.Tests;

public class ApiClientTests
{
    // ---------- test doubles ----------

    private sealed class FakeNav : NavigationManager
    {
        public string? Last;
        public FakeNav() => Initialize("http://localhost/", "http://localhost/");
        protected override void NavigateToCore(string uri, NavigationOptions options) => Last = uri;
    }

    private sealed class MemoryStore : ISessionStore
    {
        public AuthSession.Stored? Value;
        public Task<AuthSession.Stored?> GetAsync() => Task.FromResult(Value);
        public Task SetAsync(AuthSession.Stored value) { Value = value; return Task.CompletedTask; }
        public Task DeleteAsync() { Value = null; return Task.CompletedTask; }
    }

    /// <summary>Answers requests with a function; records what was sent.</summary>
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public readonly List<(string Path, string? Auth, string? Body)> Calls = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request.RequestUri!.AbsolutePath, request.Headers.Authorization?.Parameter, body));
            return respond(request);
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private const string MeJson = """
        {"id":"u1","email":"pm@test.local","fullName":"Hafiz Ismail","mustChangePassword":false,"roles":["ProjectManager"],
         "membership":{"companyId":1,"company":"Binaan Hijau","companyStatus":"Verified","role":"ProjectManager","isActive":true}}
        """;

    private static (ApiClient Api, AuthSession Session, MemoryStore Store, FakeNav Nav, Handler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var store = new MemoryStore();
        var session = new AuthSession(store, NullLogger<AuthSession>.Instance);
        var nav = new FakeNav();
        var handler = new Handler(respond);
        var api = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api/") }, session, nav);
        return (api, session, store, nav, handler);
    }

    private static async Task SignInAsync(AuthSession session, string access = "old", int expiresIn = 3600)
    {
        var me = System.Text.Json.JsonSerializer.Deserialize<MeDto>(MeJson, ApiClient.Json)!;
        await session.SetAsync(new TokenResponse("Bearer", access, expiresIn, "refresh-1"), me);
    }

    // ---------- tests ----------

    [Fact]
    public async Task Login_stores_tokens_and_profile()
    {
        var (api, session, store, _, handler) = Create(req => req.RequestUri!.AbsolutePath switch
        {
            "/api/auth/login" => Json("""{"tokenType":"Bearer","accessToken":"acc","expiresIn":3600,"refreshToken":"ref"}"""),
            "/api/auth/me" => Json(MeJson),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });

        var error = await api.LoginAsync("pm@test.local", "Passw0rd2026X");

        Assert.Null(error);
        Assert.True(session.IsAuthenticated);
        Assert.True(session.IsPm);
        Assert.Equal("Project Manager", session.RoleLabel);
        Assert.Equal("acc", store.Value!.AccessToken);
        Assert.Equal("acc", handler.Calls.Single(c => c.Path == "/api/auth/me").Auth);
    }

    [Fact]
    public async Task Wrong_password_gives_a_plain_message()
    {
        var (api, session, _, _, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var error = await api.LoginAsync("pm@test.local", "nope");
        Assert.NotNull(error);
        Assert.Contains("wrong", error);
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task Validation_errors_are_joined_into_one_line()
    {
        var (api, session, _, _, _) = Create(_ => Json(
            """{"title":"One or more validation errors occurred.","errors":{"company":["SSM number must be 12 digits.","CIDB grade must be G1 to G7."]}}""",
            HttpStatusCode.BadRequest));
        await SignInAsync(session);

        var r = await api.PostAsync("api/projects", new { });

        Assert.False(r.Ok);
        Assert.Equal("SSM number must be 12 digits. CIDB grade must be G1 to G7.", r.Error);
    }

    [Fact]
    public async Task Problem_detail_is_shown_for_rule_violations()
    {
        var (api, session, _, _, _) = Create(_ => Json(
            """{"title":"Not allowed","detail":"KLT-CO-0001: upload at least one evidence file before sending for review.","status":400}""",
            HttpStatusCode.BadRequest));
        await SignInAsync(session);

        var r = await api.PostAsync<MoveResult>("api/tickets/1/move", new { to = TicketStatus.InReview });

        Assert.Contains("evidence", r.Error);
        Assert.Equal(HttpStatusCode.BadRequest, r.Status);
    }

    [Fact]
    public async Task Too_many_requests_has_a_friendly_message()
    {
        var (api, _, _, _, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var r = await api.AnonymousPostAsync<object>("api/companies/register", new { });
        Assert.Contains("Too many attempts", r.Error);
    }

    [Fact]
    public async Task Rejected_token_is_refreshed_and_the_call_retried()
    {
        var (api, session, store, _, handler) = Create(req => req.RequestUri!.AbsolutePath switch
        {
            "/api/auth/refresh" => Json("""{"tokenType":"Bearer","accessToken":"new","expiresIn":3600,"refreshToken":"refresh-2"}"""),
            "/api/projects" when req.Headers.Authorization?.Parameter == "old" => new HttpResponseMessage(HttpStatusCode.Unauthorized),
            "/api/projects" => Json("[]"),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        await SignInAsync(session, "old");

        var r = await api.GetAsync<List<ProjectListItem>>("api/projects");

        Assert.True(r.Ok);
        Assert.Equal(["old", "new"], handler.Calls.Where(c => c.Path == "/api/projects").Select(c => c.Auth).ToArray());
        Assert.Equal("refresh-2", store.Value!.RefreshToken);
    }

    [Fact]
    public async Task Expired_token_is_refreshed_before_the_call()
    {
        var (api, session, _, _, handler) = Create(req => req.RequestUri!.AbsolutePath switch
        {
            "/api/auth/refresh" => Json("""{"tokenType":"Bearer","accessToken":"fresh","expiresIn":3600,"refreshToken":"r2"}"""),
            _ => Json("[]"),
        });
        await SignInAsync(session, "stale", expiresIn: 0);

        await api.GetAsync<List<ProjectListItem>>("api/projects");

        Assert.Equal("/api/auth/refresh", handler.Calls[0].Path);
        Assert.Equal("fresh", handler.Calls[1].Auth);
    }

    [Fact]
    public async Task Failed_refresh_signs_out_and_goes_to_login()
    {
        var (api, session, store, nav, _) = Create(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        await SignInAsync(session);

        var r = await api.GetAsync<List<ProjectListItem>>("api/projects");

        Assert.False(r.Ok);
        Assert.False(session.IsAuthenticated);
        Assert.Null(store.Value);
        Assert.EndsWith("/login?expired=1", nav.Last);
    }

    [Fact]
    public async Task Enums_are_sent_as_text()
    {
        var (api, session, _, _, handler) = Create(_ => Json("""{"id":1,"key":"KLT-CO-0001","status":"InProgress"}"""));
        await SignInAsync(session);

        await api.PostAsync<MoveResult>("api/tickets/1/move", new { to = TicketStatus.InProgress });

        Assert.Contains("\"InProgress\"", handler.Calls.Last().Body);
    }

    [Fact]
    public async Task Upload_sends_the_file_as_multipart_field_named_file()
    {
        var (api, session, _, _, handler) = Create(_ => Json("""{"id":7}""", HttpStatusCode.Created));
        await SignInAsync(session);

        using var content = new MemoryStream("hello"u8.ToArray());
        var r = await api.UploadAsync<CreatedRef>("api/tickets/1/evidence", content, "bill.pdf", "application/pdf");

        Assert.True(r.Ok);
        Assert.Equal(7, r.Value!.Id);
        var body = handler.Calls.Last().Body!;
        Assert.Contains("name=file", body);
        Assert.Contains("filename=bill.pdf", body);
    }

    [Fact]
    public async Task Server_down_returns_an_error_instead_of_crashing()
    {
        var (api, session, _, _, _) = Create(_ => throw new HttpRequestException("Connection refused"));
        await SignInAsync(session);

        var r = await api.GetAsync<List<ProjectListItem>>("api/projects");

        Assert.False(r.Ok);
        Assert.Contains("could not be reached", r.Error);
    }

    [Fact]
    public async Task Session_survives_a_reload_and_sign_out_clears_it()
    {
        var store = new MemoryStore();
        var first = new AuthSession(store, NullLogger<AuthSession>.Instance);
        await SignInAsync(first);

        var reloaded = new AuthSession(store, NullLogger<AuthSession>.Instance);
        await reloaded.LoadAsync();
        Assert.True(reloaded.IsAuthenticated);
        Assert.Equal("Hafiz Ismail", reloaded.DisplayName);

        await reloaded.SignOutAsync();
        Assert.Null(store.Value);
    }
}

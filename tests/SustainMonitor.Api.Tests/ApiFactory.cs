using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace SustainMonitor.Api.Tests;

/// <summary>
/// Runs the real API in memory against its own temporary SQLite database and evidence folder.
/// One factory (one database) per test class.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin2026Pass";
    public const string Password = "Passw0rd2026X";

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sm-api-tests-" + Guid.NewGuid().ToString("N"));

    public string DatabasePath => Path.Combine(_dir, "test.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dir);
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={DatabasePath}");
        builder.UseSetting("Storage:EvidenceFolder", Path.Combine(_dir, "evidence"));
        builder.UseSetting("DataProtection:KeysFolder", Path.Combine(_dir, "keys"));
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("UseHttpsRedirection", "false");
        builder.UseSetting("RateLimits:AuthPerMinute", "100000");
        builder.UseSetting("RateLimits:PublicPerMinute", "100000");
        builder.UseSetting("Smtp:Host", ""); // emails go to the log
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    public HttpClient Anonymous() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = Anonymous();
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.True(res.IsSuccessStatusCode, $"Login {email} failed: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }

    public Task<HttpClient> AdminAsync() => LoginAsync(AdminEmail, AdminPassword);
}

public static class Http
{
    public static readonly JsonSerializerOptions Json = Create();

    private static JsonSerializerOptions Create()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    public static Task<HttpResponseMessage> PostJson(this HttpClient c, string url, object? body = null) =>
        c.PostAsync(url, body is null ? null : JsonContent.Create(body, options: Json));

    public static Task<HttpResponseMessage> PutJson(this HttpClient c, string url, object body) =>
        c.PutAsync(url, JsonContent.Create(body, options: Json));

    public static async Task<JsonElement> Json200(this HttpResponseMessage res)
    {
        var text = await res.Content.ReadAsStringAsync();
        Assert.True(res.IsSuccessStatusCode, $"Expected success, got {(int)res.StatusCode}: {text}");
        return text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    public static async Task<JsonElement> GetJson(this HttpClient c, string url) => await (await c.GetAsync(url)).Json200();

    public static async Task<string> Fails(this HttpResponseMessage res, HttpStatusCode expected)
    {
        var text = await res.Content.ReadAsStringAsync();
        Assert.True(res.StatusCode == expected, $"Expected {(int)expected}, got {(int)res.StatusCode}: {text}");
        return text;
    }

    public static int Int(this JsonElement e, string name) => e.GetProperty(name).GetInt32();
    public static string Str(this JsonElement e, string name) => e.GetProperty(name).GetString()!;
}

/// <summary>Builds a verified company with CEO, Project Manager and Superior, all signed in.</summary>
public sealed class CompanyTeam
{
    public required int CompanyId { get; init; }
    public required string CeoEmail { get; init; }
    public required string PmEmail { get; init; }
    public required string SupEmail { get; init; }
    public required HttpClient Ceo { get; init; }
    public required HttpClient Pm { get; init; }
    public required HttpClient Sup { get; init; }

    private static int _seq;

    public static string Unique(string prefix) => $"{prefix}{Interlocked.Increment(ref _seq)}{Guid.NewGuid().ToString("N")[..6]}";

    /// <summary>12-digit SSM number, unique per call.</summary>
    public static string Ssm() => "2026" + Random.Shared.NextInt64(10_000_000, 99_999_999);

    public static async Task<HttpResponseMessage> RegisterAsync(ApiFactory f, string ceoEmail, string? ssm = null) =>
        await f.Anonymous().PostJson("/api/companies/register", new
        {
            companyName = "Test Builders " + Unique("C"),
            ssmNo = ssm ?? Ssm(),
            cidbGrade = "G5",
            ceoName = "Test CEO",
            ceoEmail,
            password = ApiFactory.Password,
        });

    public static async Task<CompanyTeam> CreateAsync(ApiFactory f)
    {
        var ceoEmail = Unique("ceo") + "@test.local";
        var reg = await (await RegisterAsync(f, ceoEmail)).Json200();
        var companyId = reg.Int("id");

        var admin = await f.AdminAsync();
        await (await admin.PostJson($"/api/admin/companies/{companyId}/verify")).Json200();

        var ceo = await f.LoginAsync(ceoEmail, ApiFactory.Password);
        var pmEmail = Unique("pm") + "@test.local";
        var supEmail = Unique("sup") + "@test.local";
        await (await ceo.PostJson("/api/company/users", new { fullName = "Test PM", email = pmEmail, role = "ProjectManager", temporaryPassword = "Temp2026Pass" })).Json200();
        await (await ceo.PostJson("/api/company/users", new { fullName = "Test Superior", email = supEmail, role = "Superior", temporaryPassword = "Temp2026Pass" })).Json200();

        return new CompanyTeam
        {
            CompanyId = companyId,
            CeoEmail = ceoEmail,
            PmEmail = pmEmail,
            SupEmail = supEmail,
            Ceo = ceo,
            Pm = await FirstLoginAsync(f, pmEmail),
            Sup = await FirstLoginAsync(f, supEmail),
        };
    }

    /// <summary>Signs in with the temporary password, changes it, signs in again.</summary>
    public static async Task<HttpClient> FirstLoginAsync(ApiFactory f, string email)
    {
        var temp = await f.LoginAsync(email, "Temp2026Pass");
        await (await temp.PostJson("/api/auth/change-password", new { currentPassword = "Temp2026Pass", newPassword = ApiFactory.Password })).Json200();
        return await f.LoginAsync(email, ApiFactory.Password);
    }

    /// <summary>Creates an NREB project and returns (projectId, criterion id by code).</summary>
    public async Task<(int ProjectId, Dictionary<string, int> Criteria)> CreateProjectAsync(decimal gfa = 1200)
    {
        var tools = await Pm.GetJson("/api/tools");
        var nreb = tools.EnumerateArray().Single(t => t.Str("code") == "NREB").Int("id");
        var tool = await Pm.GetJson($"/api/tools/{nreb}");
        var criteria = tool.GetProperty("categories").EnumerateArray()
            .SelectMany(c => c.GetProperty("criteria").EnumerateArray())
            .ToDictionary(c => c.Str("code"), c => c.Int("id"));

        var code = "P" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var created = await (await Pm.PostJson("/api/projects", new { code, name = "Test project " + code, toolId = nreb, gfaM2 = gfa, targetRating = "Certified" })).Json200();
        return (created.Int("id"), criteria);
    }

    /// <summary>The worked example: 47 of 55 targeted points, WE1 (3 points) not yet done.</summary>
    public static object[] Baseline47(Dictionary<string, int> c) =>
    [
        S(c["EE1"], 2, 2), S(c["EE2"], 3, 3), S(c["EE3"], 2, 2), S(c["EE4"], 5, 5), S(c["EE5"], 15, 10, "InProgress"),
        S(c["EE6"], 4, 4), S(c["EE9"], 3, 3), S(c["EQ1"], 1, 1), S(c["EQ4"], 2, 2), S(c["EQ8"], 2, 2), S(c["EQ12"], 2, 2),
        S(c["SM6"], 4, 4), S(c["MR5"], 3, 3), S(c["WE1"], 3, 0, "NotStarted"), S(c["WE4"], 3, 3), S(c["IN2"], 1, 1),
    ];

    private static object S(int id, int target, int awarded, string status = "Achieved") =>
        new { criterionId = id, status, pointsTargeted = target, pointsAwarded = awarded };
}

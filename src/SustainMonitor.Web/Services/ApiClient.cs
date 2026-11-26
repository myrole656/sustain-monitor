using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using SustainMonitor.Web.Models;

namespace SustainMonitor.Web.Services;

public sealed record ApiResult<T>(bool Ok, T? Value, string? Error, HttpStatusCode Status)
{
    public static ApiResult<T> Fail(string error, HttpStatusCode status) => new(false, default, error, status);
}

/// <summary>
/// Talks to SustainMonitor.Api with the signed-in user's bearer token. Renews the token with the refresh
/// token when it expires; signs out and goes to the login page if that fails.
/// </summary>
public sealed class ApiClient(HttpClient http, AuthSession session, NavigationManager nav)
{
    public static readonly JsonSerializerOptions Json = CreateJson();

    private static JsonSerializerOptions CreateJson()
    {
        var o = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        o.Converters.Add(new JsonStringEnumConverter());
        return o;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, url));

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = body is null ? null : JsonContent.Create(body, options: Json),
        });

    public async Task<ApiResult<bool>> PostAsync(string url, object? body = null)
    {
        var r = await SendAsync<JsonElement>(() => new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = body is null ? null : JsonContent.Create(body, options: Json),
        });
        return new ApiResult<bool>(r.Ok, r.Ok, r.Error, r.Status);
    }

    public Task<ApiResult<T>> UploadAsync<T>(string url, Stream file, string fileName, string contentType) =>
        SendAsync<T>(() =>
        {
            var form = new MultipartFormDataContent();
            var part = new StreamContent(file);
            part.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var ct) ? ct : new("application/octet-stream");
            form.Add(part, "file", fileName);
            return new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        }, retryable: false);

    public async Task<(Stream? Content, string? Error)> DownloadAsync(string url)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var res = await SendRawAsync(req);
        if (!res.IsSuccessStatusCode) return (null, await ReadErrorAsync(res));
        var ms = new MemoryStream();
        await res.Content.CopyToAsync(ms);
        ms.Position = 0;
        return (ms, null);
    }

    /// <summary>Calls that need no token (login, register, verify, forgot password).</summary>
    public async Task<ApiResult<T>> AnonymousPostAsync<T>(string url, object body)
    {
        using var res = await http.PostAsJsonAsync(url, body, Json);
        return await ToResultAsync<T>(res);
    }

    public async Task<ApiResult<T>> AnonymousGetAsync<T>(string url)
    {
        using var res = await http.GetAsync(url);
        return await ToResultAsync<T>(res);
    }

    /// <summary>Logs in, loads /me and stores the session.</summary>
    public async Task<string?> LoginAsync(string email, string password)
    {
        using var res = await http.PostAsJsonAsync("api/auth/login", new { email, password }, Json);
        if (res.StatusCode == HttpStatusCode.Unauthorized)
            return "Email or password is wrong, or the account is locked. After 5 wrong tries, wait 15 minutes.";
        if (!res.IsSuccessStatusCode) return await ReadErrorAsync(res);
        var tokens = (await res.Content.ReadFromJsonAsync<TokenResponse>(Json))!;

        using var meReq = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        using var meRes = await http.SendAsync(meReq);
        if (!meRes.IsSuccessStatusCode) return await ReadErrorAsync(meRes);
        var me = (await meRes.Content.ReadFromJsonAsync<MeDto>(Json))!;
        await session.SetAsync(tokens, me);
        return null;
    }

    public async Task RefreshMeAsync()
    {
        var r = await GetAsync<MeDto>("api/auth/me");
        if (r.Ok && r.Value is not null) await session.UpdateMeAsync(r.Value);
    }

    public async Task LogoutAsync()
    {
        await session.SignOutAsync();
        nav.NavigateTo("/login", forceLoad: false);
    }

    // ---------------- internals ----------------

    private async Task<ApiResult<T>> SendAsync<T>(Func<HttpRequestMessage> build, bool retryable = true)
    {
        try
        {
            using var req = build();
            using var res = await SendRawAsync(req, retryable ? build : null);
            return await ToResultAsync<T>(res);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Fail($"The server could not be reached ({ex.Message}).", HttpStatusCode.ServiceUnavailable);
        }
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage req, Func<HttpRequestMessage>? rebuild = null)
    {
        if (session.IsAuthenticated && session.AccessTokenExpired) await TryRefreshAsync();
        Authorize(req);
        var res = await http.SendAsync(req);
        if (res.StatusCode != HttpStatusCode.Unauthorized || !session.IsAuthenticated) return res;

        // Token rejected: try one refresh, then retry once.
        if (rebuild is not null && await TryRefreshAsync())
        {
            res.Dispose();
            var again = rebuild();
            Authorize(again);
            res = await http.SendAsync(again);
            if (res.StatusCode != HttpStatusCode.Unauthorized) return res;
        }

        await session.SignOutAsync();
        nav.NavigateTo("/login?expired=1");
        return res;
    }

    private void Authorize(HttpRequestMessage req)
    {
        if (session.AccessToken is { } token)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<bool> TryRefreshAsync()
    {
        if (session.RefreshToken is not { } refresh) return false;
        using var res = await http.PostAsJsonAsync("api/auth/refresh", new { refreshToken = refresh }, Json);
        if (!res.IsSuccessStatusCode) return false;
        var tokens = await res.Content.ReadFromJsonAsync<TokenResponse>(Json);
        if (tokens is null) return false;
        await session.UpdateTokensAsync(tokens);
        return true;
    }

    private static async Task<ApiResult<T>> ToResultAsync<T>(HttpResponseMessage res)
    {
        if (!res.IsSuccessStatusCode) return ApiResult<T>.Fail(await ReadErrorAsync(res), res.StatusCode);
        if (res.StatusCode == HttpStatusCode.NoContent || res.Content.Headers.ContentLength == 0)
            return new ApiResult<T>(true, default, null, res.StatusCode);
        var value = await res.Content.ReadFromJsonAsync<T>(Json);
        return new ApiResult<T>(true, value, null, res.StatusCode);
    }

    /// <summary>Turns ProblemDetails / ValidationProblem into one readable line.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage res)
    {
        if (res.StatusCode == HttpStatusCode.TooManyRequests) return "Too many attempts. Wait a minute and try again.";
        try
        {
            var doc = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
            if (doc.ValueKind == JsonValueKind.Object)
            {
                if (doc.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                            ? p.Value.EnumerateArray().Select(v => v.GetString())
                            : [p.Value.ToString()])
                        .Where(m => !string.IsNullOrWhiteSpace(m));
                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0) return joined;
                }
                if (doc.TryGetProperty("detail", out var d) && d.GetString() is { Length: > 0 } detail) return detail;
                if (doc.TryGetProperty("title", out var t) && t.GetString() is { Length: > 0 } title) return title;
            }
        }
        catch (JsonException) { }
        catch (NotSupportedException) { }
        return res.StatusCode switch
        {
            HttpStatusCode.Forbidden => "You do not have permission for this.",
            HttpStatusCode.NotFound => "Not found.",
            _ => $"Request failed ({(int)res.StatusCode}).",
        };
    }
}

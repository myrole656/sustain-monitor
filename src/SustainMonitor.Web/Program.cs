using Microsoft.AspNetCore.DataProtection;
using SustainMonitor.Web.Components;
using SustainMonitor.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Sessions are encrypted with Data Protection; persist keys so users stay signed in across restarts.
var keys = builder.Services.AddDataProtection().SetApplicationName("SustainMonitor.Web");
if (config["DataProtection:KeysFolder"] is { Length: > 0 } keysFolder)
    keys.PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

var apiBase = config["Api:BaseUrl"] ?? "https://localhost:7180/";
if (!apiBase.EndsWith('/')) apiBase += "/";

builder.Services.AddScoped<ISessionStore, BrowserSessionStore>();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddHttpClient<ApiClient>(c =>
{
    c.BaseAddress = new Uri(apiBase);
    c.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (config.GetValue("UseHttpsRedirection", true)) app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "same-origin";
    await next();
});

app.UseAntiforgery();
app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

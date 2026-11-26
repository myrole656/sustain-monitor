using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using SustainMonitor.Api.Endpoints;
using SustainMonitor.Api.Services;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Certificates;
using SustainMonitor.Domain.Companies;
using SustainMonitor.Domain.Projects;
using SustainMonitor.Domain.Scoring;
using SustainMonitor.Domain.Tickets;
using SustainMonitor.Infrastructure;
using SustainMonitor.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Keys: tokens must survive restarts and container rebuilds ----------
var keysFolder = config["DataProtection:KeysFolder"];
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SustainMonitor.Api");
if (!string.IsNullOrWhiteSpace(keysFolder))
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

// ---------- Data, storage, email ----------
builder.Services.AddInfrastructure(config, builder.Environment.ContentRootPath);
builder.Services.AddScoped<Notifier>();

// ---------- Authentication (ASP.NET Core Identity, bearer tokens + refresh) ----------
builder.Services
    .AddIdentityApiEndpoints<AppUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 10;
        o.Password.RequireDigit = true;
        o.Password.RequireUppercase = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.AllowedForNewUsers = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, o =>
{
    o.BearerTokenExpiration = TimeSpan.FromMinutes(config.GetValue("Auth:AccessTokenMinutes", 60));
    o.RefreshTokenExpiration = TimeSpan.FromDays(config.GetValue("Auth:RefreshTokenDays", 14));
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("PlatformAdmin", p => p.RequireRole(Roles.PlatformAdmin));

// ---------- Rate limits: sign-in and public pages ----------
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = config.GetValue("RateLimits:AuthPerMinute", 20), Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = config.GetValue("RateLimits:PublicPerMinute", 30), Window = TimeSpan.FromMinutes(1) }));
});

// ---------- CORS for the web front end ----------
var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (origins.Length > 0) p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));

// ---------- Platform ----------
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services, app.Configuration);

// Business-rule violations from the domain become 400s with the rule's message.
app.UseExceptionHandler(handler => handler.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var isRule = ex is ScoringException or TicketWorkflowException or ECertificateException or CompanyException or ChecklistException or InvalidDataException;
    var status = isRule ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
    await Results.Problem(
        title: isRule ? "Not allowed" : "Unexpected error",
        detail: isRule ? ex!.Message : null,
        statusCode: status).ExecuteAsync(ctx);
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // /openapi/v1.json
    app.MapScalarApiReference();      // /scalar — interactive API docs
}
else
{
    app.UseHsts();
}

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    await next();
});

if (config.GetValue("UseHttpsRedirection", true)) app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AccountGateMiddleware>();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { name = "Sustain Monitor API", status = "ok" })).ExcludeFromDescription();
app.MapHealthChecks("/health");

// Login, refresh, forgot/reset password, plus /me and /change-password.
// Open self-registration is switched off: people join through company registration (CEO) or a CEO invite.
var auth = app.MapGroup("/api/auth").WithTags("Auth").RequireRateLimiting("auth");
auth.AddEndpointFilter(async (ctx, next) =>
    ctx.HttpContext.Request.Path.Value?.EndsWith("/register", StringComparison.OrdinalIgnoreCase) == true
        ? Results.NotFound()
        : await next(ctx));
auth.MapIdentityApi<AppUser>();
auth.MapAccountEndpoints();

app.MapCompanyEndpoints();
app.MapAdminEndpoints();
app.MapCatalogueEndpoints();
app.MapProjectEndpoints();
app.MapChecklistEndpoints();
app.MapTicketEndpoints();
app.MapCertificateEndpoints();
app.MapNotificationEndpoints();

app.Run();

/// <summary>/health reports Unhealthy when the SQLite file cannot be opened.</summary>
public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        await db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy("SQLite reachable")
            : HealthCheckResult.Unhealthy("SQLite not reachable");
}

public partial class Program;

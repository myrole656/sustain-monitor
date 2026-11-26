using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Who am I, and first-login password change. Login itself is Identity's POST /api/auth/login.</summary>
public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this RouteGroupBuilder auth)
    {
        auth.MapGet("/me", async (ClaimsPrincipal user, UserManager<AppUser> users, AppDbContext db) =>
        {
            var u = await users.GetUserAsync(user);
            if (u is null) return Results.Unauthorized();
            var roles = await users.GetRolesAsync(u);
            var membership = await db.CompanyUsers.AsNoTracking()
                .Where(x => x.UserId == u.Id)
                .Select(x => new { x.CompanyId, Company = x.Company!.Name, CompanyStatus = x.Company.Status, x.Role, x.IsActive })
                .SingleOrDefaultAsync();
            return Results.Ok(new { u.Id, u.Email, u.FullName, u.MustChangePassword, Roles = roles, Membership = membership });
        }).RequireAuthorization();

        auth.MapPost("/change-password", async (ChangePasswordRequest req, ClaimsPrincipal user, UserManager<AppUser> users) =>
        {
            var u = await users.GetUserAsync(user);
            if (u is null) return Results.Unauthorized();
            if (req.CurrentPassword == req.NewPassword)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["newPassword"] = ["Choose a different password."] });
            var result = await users.ChangePasswordAsync(u, req.CurrentPassword, req.NewPassword);
            if (!result.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = result.Errors.Select(e => e.Description).ToArray() });
            u.MustChangePassword = false;
            await users.UpdateAsync(u);
            return Results.Ok(new { Message = "Password changed." });
        }).RequireAuthorization();
    }
}

/// <summary>
/// Runs after authentication on every request: rejects tokens of locked accounts (deactivated users,
/// suspended companies) and holds invited users at "change your password" until they do.
/// </summary>
public sealed class AccountGateMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, UserManager<AppUser> users)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var u = await users.GetUserAsync(ctx.User);
            if (u is null || await users.IsLockedOutAsync(u))
            {
                await Results.Problem("This account is locked or no longer exists.", statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(ctx);
                return;
            }

            var path = ctx.Request.Path;
            var allowed = path.StartsWithSegments("/api/auth/change-password") || path.StartsWithSegments("/api/auth/me")
                          || path.StartsWithSegments("/api/auth/refresh");
            if (u.MustChangePassword && !allowed)
            {
                await Results.Problem("Change your temporary password first: POST /api/auth/change-password.",
                    statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(ctx);
                return;
            }
        }
        await next(ctx);
    }
}

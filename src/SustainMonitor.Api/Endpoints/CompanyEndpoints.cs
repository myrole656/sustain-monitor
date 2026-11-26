using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Api.Services;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Companies;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

public record RegisterCompanyRequest(
    string CompanyName, string SsmNo, string CidbGrade, string? Address, string? Phone,
    string CeoName, string CeoEmail, string Password);

public record InviteUserRequest(string FullName, string Email, CompanyRole Role, string TemporaryPassword);

public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        // Step 1 of the flow: a construction company signs up; its CEO account is created with it.
        app.MapPost("/api/companies/register", async (RegisterCompanyRequest req, AppDbContext db,
            UserManager<AppUser> users, Notifier notifier) =>
        {
            var errors = CompanyRules.ValidateRegistration(req.CompanyName, req.SsmNo, req.CidbGrade).ToList();
            if (string.IsNullOrWhiteSpace(req.CeoName)) errors.Add("CEO name is required.");
            if (string.IsNullOrWhiteSpace(req.CeoEmail)) errors.Add("CEO email is required.");
            var ssm = (req.SsmNo ?? "").Trim().ToUpperInvariant();
            if (errors.Count == 0 && await db.Companies.AnyAsync(c => c.SsmNo == ssm))
                errors.Add("A company with this SSM number is already registered.");
            if (errors.Count > 0) return Results.ValidationProblem(new Dictionary<string, string[]> { ["company"] = errors.ToArray() });

            await using var tx = await db.Database.BeginTransactionAsync();
            var ceo = new AppUser { UserName = req.CeoEmail.Trim(), Email = req.CeoEmail.Trim(), FullName = req.CeoName.Trim() };
            var created = await users.CreateAsync(ceo, req.Password);
            if (!created.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["ceo"] = created.Errors.Select(e => e.Description).ToArray() });
            await users.AddToRoleAsync(ceo, Roles.CEO);

            var company = new Company
            {
                Name = req.CompanyName.Trim(),
                SsmNo = ssm,
                CidbGrade = req.CidbGrade.Trim().ToUpperInvariant(),
                Address = req.Address,
                Phone = req.Phone,
            };
            company.Users.Add(new CompanyUser { UserId = ceo.Id, FullName = ceo.FullName!, Role = CompanyRole.CEO });
            db.Companies.Add(company);
            db.AuditLogs.Add(new AuditLog { UserId = ceo.Id, Action = "Register", Entity = nameof(Company), EntityId = ssm });
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            await notifier.NotifyAsync(await notifier.PlatformAdminsAsync(), "company.registered",
                $"New company to verify: {company.Name}",
                $"{company.Name} (SSM {company.SsmNo}, CIDB {company.CidbGrade}) registered and is waiting for verification.",
                new { company.Id });

            return Results.Created("/api/company", new
            {
                company.Id, company.Name, company.Status,
                Message = "Registered. A platform admin will verify your company before you can add your team and projects.",
            });
        }).AllowAnonymous().RequireRateLimiting("public").WithTags("Company");

        var g = app.MapGroup("/api/company").WithTags("Company").RequireAuthorization();

        g.MapGet("/", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            var team = await db.CompanyUsers.AsNoTracking()
                .Where(u => u.CompanyId == m.CompanyId)
                .OrderBy(u => u.Role).ThenBy(u => u.FullName)
                .Select(u => new { u.UserId, u.FullName, u.Role, u.IsActive, u.JoinedAt })
                .ToListAsync();
            return Results.Ok(new
            {
                m.Company.Id, m.Company.Name, m.Company.SsmNo, m.Company.CidbGrade, m.Company.Address, m.Company.Phone,
                m.Company.Status, m.Company.StatusNote, MyRole = m.Role, Team = team,
            });
        });

        // CEO adds a Project Manager or Superior with a temporary password; they must change it at first login.
        g.MapPost("/users", async (InviteUserRequest req, ClaimsPrincipal user, AppDbContext db,
            UserManager<AppUser> users, Notifier notifier) =>
        {
            var m = await db.GetMembershipAsync(user);
            if (m is null) return MembershipExtensions.NotMember();
            CompanyRules.EnsureCanInvite(m.Role, req.Role);
            CompanyRules.EnsureActive(m.Company);
            if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Email))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["user"] = ["Name and email are required."] });

            await using var tx = await db.Database.BeginTransactionAsync();
            var person = new AppUser
            {
                UserName = req.Email.Trim(), Email = req.Email.Trim(), FullName = req.FullName.Trim(),
                MustChangePassword = true,
            };
            var created = await users.CreateAsync(person, req.TemporaryPassword);
            if (!created.Succeeded)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["user"] = created.Errors.Select(e => e.Description).ToArray() });
            await users.AddToRoleAsync(person, Roles.For(req.Role));

            db.CompanyUsers.Add(new CompanyUser { CompanyId = m.CompanyId, UserId = person.Id, FullName = person.FullName!, Role = req.Role });
            db.AuditLogs.Add(new AuditLog { UserId = m.UserId, Action = $"Invite {req.Role}", Entity = nameof(CompanyUser), EntityId = person.Id });
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            await notifier.EmailOnlyAsync(person.Email!, $"You have been added to {m.Company.Name} on Sustain Monitor",
                $"Hello {person.FullName},\n\nYou were added as {req.Role} for {m.Company.Name}.\n\n" +
                $"Sign in with: {person.Email}\nTemporary password: {req.TemporaryPassword}\n\n" +
                "You will be asked to choose your own password at first sign-in.\n\n— Sustain Monitor");

            return Results.Created("/api/company", new { person.Id, person.Email, req.Role, MustChangePassword = true });
        });

        g.MapPost("/users/{userId}/deactivate", (string userId, ClaimsPrincipal user, AppDbContext db, UserManager<AppUser> users) =>
            SetActiveAsync(userId, false, user, db, users));

        g.MapPost("/users/{userId}/reactivate", (string userId, ClaimsPrincipal user, AppDbContext db, UserManager<AppUser> users) =>
            SetActiveAsync(userId, true, user, db, users));
    }

    private static async Task<IResult> SetActiveAsync(string userId, bool active, ClaimsPrincipal user, AppDbContext db, UserManager<AppUser> users)
    {
        var m = await db.GetMembershipAsync(user);
        if (m is null) return MembershipExtensions.NotMember();
        if (m.Role != CompanyRole.CEO) return MembershipExtensions.NotAllowed("change team members");
        var target = await db.CompanyUsers.SingleOrDefaultAsync(u => u.CompanyId == m.CompanyId && u.UserId == userId);
        if (target is null) return Results.NotFound();
        if (target.Role == CompanyRole.CEO) return MembershipExtensions.NotAllowed("deactivate the CEO");

        target.IsActive = active;
        var account = await users.FindByIdAsync(userId);
        if (account is not null) await AccountLock.SetAsync(users, account, locked: !active);

        db.AuditLogs.Add(new AuditLog { UserId = m.UserId, Action = active ? "Reactivate" : "Deactivate", Entity = nameof(CompanyUser), EntityId = userId });
        await db.SaveChangesAsync();
        return Results.Ok(new { target.UserId, target.IsActive });
    }
}

/// <summary>Locking an account stops sign-in and, via the account gate, rejects tokens already issued.</summary>
public static class AccountLock
{
    public static async Task SetAsync(UserManager<AppUser> users, AppUser account, bool locked)
    {
        await users.SetLockoutEnabledAsync(account, true);
        await users.SetLockoutEndDateAsync(account, locked ? DateTimeOffset.MaxValue : null);
    }
}

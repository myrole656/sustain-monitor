using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SustainMonitor.Domain;
using SustainMonitor.Domain.Entities;
using SustainMonitor.Infrastructure.Data;

namespace SustainMonitor.Api.Endpoints;

/// <summary>The signed-in person's company and role.</summary>
public sealed record Membership(string UserId, int CompanyId, CompanyRole Role, Company Company);

public static class MembershipExtensions
{
    public static string UserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    public static async Task<Membership?> GetMembershipAsync(this AppDbContext db, ClaimsPrincipal user)
    {
        var id = user.UserId();
        var cu = await db.CompanyUsers.Include(x => x.Company)
            .SingleOrDefaultAsync(x => x.UserId == id && x.IsActive);
        return cu is null ? null : new Membership(id, cu.CompanyId, cu.Role, cu.Company!);
    }

    /// <summary>Loads a project only if it belongs to the caller's company.</summary>
    public static Task<Project?> FindCompanyProjectAsync(this AppDbContext db, int projectId, Membership m) =>
        db.Projects.SingleOrDefaultAsync(p => p.Id == projectId && p.CompanyId == m.CompanyId);

    public static Task<Assessment?> CurrentAssessmentAsync(this AppDbContext db, int projectId) =>
        db.Assessments.Include(a => a.CriterionScores).Include(a => a.CategoryScores)
            .SingleOrDefaultAsync(a => a.ProjectId == projectId && a.IsCurrent);

    public static IResult NotMember() =>
        Results.Problem("You are not an active member of a company.", statusCode: StatusCodes.Status403Forbidden);

    public static IResult NotAllowed(string action) =>
        Results.Problem($"Your role cannot {action}.", statusCode: StatusCodes.Status403Forbidden);
}

using System.Text.RegularExpressions;
using SustainMonitor.Domain.Entities;

namespace SustainMonitor.Domain.Companies;

public sealed class CompanyException(string message) : Exception(message);

public static partial class CompanyRules
{
    // New SSM format is 12 digits (e.g. 202301012345); older numbers look like 123456-A.
    [GeneratedRegex(@"^(\d{12}|\d{1,7}-[A-Z])$")]
    private static partial Regex SsmPattern();

    public static IReadOnlyList<string> ValidateRegistration(string? name, string? ssmNo, string? cidbGrade)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add("Company name is required.");
        if (string.IsNullOrWhiteSpace(ssmNo) || !SsmPattern().IsMatch(ssmNo.Trim().ToUpperInvariant()))
            errors.Add("SSM number must be 12 digits (e.g. 202301012345) or the old format (e.g. 123456-A).");
        if (string.IsNullOrWhiteSpace(cidbGrade) || !Company.CidbGrades.Contains(cidbGrade.Trim().ToUpperInvariant()))
            errors.Add("CIDB grade must be G1 to G7.");
        return errors;
    }

    public static void Verify(Company company, string adminId, DateTime now)
    {
        if (company.Status is not (CompanyStatus.Pending or CompanyStatus.Suspended))
            throw new CompanyException($"Company is {company.Status}; only pending or suspended companies can be verified.");
        company.Status = CompanyStatus.Verified;
        company.VerifiedBy = adminId;
        company.VerifiedAt = now;
        company.StatusNote = null;
    }

    public static void Reject(Company company, string adminId, string reason, DateTime now)
    {
        if (company.Status != CompanyStatus.Pending)
            throw new CompanyException("Only pending companies can be rejected.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new CompanyException("Give a reason for rejecting.");
        company.Status = CompanyStatus.Rejected;
        company.VerifiedBy = adminId;
        company.VerifiedAt = now;
        company.StatusNote = reason;
    }

    public static void Suspend(Company company, string adminId, string reason, DateTime now)
    {
        if (company.Status != CompanyStatus.Verified)
            throw new CompanyException("Only verified companies can be suspended.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new CompanyException("Give a reason for suspending.");
        company.Status = CompanyStatus.Suspended;
        company.VerifiedBy = adminId;
        company.VerifiedAt = now;
        company.StatusNote = reason;
    }

    public static void EnsureActive(Company company)
    {
        if (company.Status != CompanyStatus.Verified)
            throw new CompanyException("Your company is not verified yet. A platform admin must verify it before you can continue.");
    }

    /// <summary>Only the CEO invites; a company has exactly one CEO.</summary>
    public static void EnsureCanInvite(CompanyRole inviterRole, CompanyRole newRole)
    {
        if (inviterRole != CompanyRole.CEO)
            throw new CompanyException("Only the CEO can add team members.");
        if (newRole == CompanyRole.CEO)
            throw new CompanyException("A company has one CEO; invite a Project Manager or Superior.");
    }
}

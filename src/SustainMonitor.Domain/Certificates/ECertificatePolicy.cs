using SustainMonitor.Domain.Entities;

namespace SustainMonitor.Domain.Certificates;

public sealed class ECertificateException(string message) : Exception(message);

/// <summary>
/// E-cert lifecycle: an analysis scoring at or above the lowest GBI band (Certified, 50) requests a certificate;
/// a platform admin approves (Issued, valid 3 years, matching GBI's re-verification cycle) or declines;
/// an issued certificate can later be revoked.
/// </summary>
public static class ECertificatePolicy
{
    public const int ValidityYears = 3;

    public static int MinimumPoints(GbiTool tool) =>
        tool.RatingBands.Count == 0 ? 50 : tool.RatingBands.Min(b => b.MinPoints);

    public static bool Qualifies(GbiTool tool, Assessment assessment) =>
        assessment.TotalPoints >= MinimumPoints(tool);

    /// <summary>Returns a new request, or null when the score is below the lowest band.</summary>
    public static ECertificate? Request(GbiTool tool, Project project, Assessment assessment, string requestedBy, DateTime now,
        IEnumerable<ECertificate> existingForProject)
    {
        if (assessment.ProjectId != project.Id)
            throw new ECertificateException("Assessment does not belong to this project.");
        if (!assessment.IsCurrent)
            throw new ECertificateException("Only the current assessment can be certified.");
        if (!Qualifies(tool, assessment)) return null;

        var pending = existingForProject.FirstOrDefault(c => c.Status == ECertStatus.Requested);
        if (pending is not null)
            throw new ECertificateException("An e-cert request for this project is already waiting for approval.");

        return new ECertificate
        {
            ProjectId = project.Id,
            AssessmentId = assessment.Id,
            Points = assessment.TotalPoints,
            Rating = assessment.Rating,
            ToolCode = tool.Code,
            ToolVersion = tool.Version,
            Status = ECertStatus.Requested,
            RequestedBy = requestedBy,
            RequestedAt = now,
        };
    }

    public static string FormatCertNo(int year, int sequence) => $"SM-{year}-{sequence:D6}";

    /// <summary>Issues the certificate. Any earlier issued certificate for the project should be revoked by the caller.</summary>
    public static void Approve(ECertificate cert, string adminId, int sequence, string verifyToken, DateTime now)
    {
        if (cert.Status != ECertStatus.Requested)
            throw new ECertificateException($"Only requested e-certs can be approved (this one is {cert.Status}).");
        if (string.IsNullOrWhiteSpace(verifyToken) || verifyToken.Length < 16)
            throw new ECertificateException("Verify token must be at least 16 characters.");
        cert.Status = ECertStatus.Issued;
        cert.CertNo = FormatCertNo(now.Year, sequence);
        cert.VerifyToken = verifyToken;
        cert.DecidedBy = adminId;
        cert.DecidedAt = now;
        cert.IssuedAt = now;
        cert.ExpiresAt = now.AddYears(ValidityYears);
    }

    public static void Decline(ECertificate cert, string adminId, string reason, DateTime now)
    {
        if (cert.Status != ECertStatus.Requested)
            throw new ECertificateException($"Only requested e-certs can be declined (this one is {cert.Status}).");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ECertificateException("Give a reason so the company knows what to fix.");
        cert.Status = ECertStatus.Declined;
        cert.DecidedBy = adminId;
        cert.DecidedAt = now;
        cert.DecisionNote = reason;
    }

    public static void Revoke(ECertificate cert, string adminId, string reason, DateTime now)
    {
        if (cert.Status != ECertStatus.Issued)
            throw new ECertificateException("Only issued e-certs can be revoked.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ECertificateException("Give a reason for revoking.");
        cert.Status = ECertStatus.Revoked;
        cert.DecidedBy = adminId;
        cert.DecidedAt = now;
        cert.DecisionNote = reason;
    }

    /// <summary>What the public verification page shows.</summary>
    public static string PublicStatus(ECertificate cert, DateTime now) => cert.Status switch
    {
        ECertStatus.Issued when cert.ExpiresAt <= now => "Expired",
        ECertStatus.Issued => "Valid",
        ECertStatus.Revoked => "Revoked",
        _ => "Not issued",
    };
}

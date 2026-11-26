namespace SustainMonitor.Domain.Entities;

/// <summary>A construction company. Only verified companies can create projects.</summary>
public class Company
{
    public int Id { get; set; }
    public required string Name { get; set; }
    /// <summary>SSM registration number.</summary>
    public required string SsmNo { get; set; }
    /// <summary>CIDB grade, G1–G7.</summary>
    public required string CidbGrade { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.Pending;
    public string? StatusNote { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CompanyUser> Users { get; set; } = [];
    public List<Project> Projects { get; set; } = [];

    public static readonly string[] CidbGrades = ["G1", "G2", "G3", "G4", "G5", "G6", "G7"];
}

/// <summary>A person's membership and role in one company.</summary>
public class CompanyUser
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public required string UserId { get; set; }
    public required string FullName { get; set; }
    public CompanyRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// A GBI-aligned self-assessment e-certificate issued by Sustain Monitor.
/// It is not an official GBI certificate; only GBI's Accreditation Panel grants those.
/// </summary>
public class ECertificate
{
    public int Id { get; set; }
    /// <summary>e.g. SM-2026-000042, set when issued.</summary>
    public string? CertNo { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public int Points { get; set; }
    public required string Rating { get; set; }
    public required string ToolCode { get; set; }
    public required string ToolVersion { get; set; }
    public ECertStatus Status { get; set; } = ECertStatus.Requested;
    public required string RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Random token in the QR code's public verification link.</summary>
    public string? VerifyToken { get; set; }
}

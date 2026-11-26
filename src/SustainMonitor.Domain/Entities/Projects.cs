namespace SustainMonitor.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    /// <summary>Short code used in ticket keys, e.g. "KLT" → KLT-CO-0012.</summary>
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Location { get; set; }
    public string? BuildingType { get; set; }
    public decimal? GfaM2 { get; set; }
    public int ToolId { get; set; }
    public GbiTool? Tool { get; set; }
    public string? TargetRating { get; set; }
    public AssessmentStage CurrentStage { get; set; } = AssessmentStage.SelfCheck;
    public DateOnly? RegDate { get; set; }
    public string? PicName { get; set; }
    public string? PicContact { get; set; }
    public DateOnly? CertExpiryDate { get; set; }
    /// <summary>The Project Manager who created the project.</summary>
    public required string CreatedById { get; set; }
    public int NextTicketNumber { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Assessment> Assessments { get; set; } = [];
    public List<Ticket> Tickets { get; set; } = [];
    public List<ProjectChecklistEntry> Checklist { get; set; } = [];
}

/// <summary>
/// An immutable scoring snapshot. A re-score never edits an assessment; it inserts a new one
/// with VersionNo + 1 and moves IsCurrent.
/// </summary>
/// <summary>A ticked lifecycle checklist item (see Projects.PhaseChecklist). Unticked items have no row.</summary>
public class ProjectChecklistEntry
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public required string ItemKey { get; set; }
    public string? CheckedById { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

public class Assessment
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public AssessmentStage Stage { get; set; }
    public int VersionNo { get; set; }
    public AssessmentTrigger Trigger { get; set; }
    public int? TriggerTicketId { get; set; }
    public int TotalPoints { get; set; }
    public string Rating { get; set; } = RatingNames.NotRated;
    public bool IsCurrent { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CriterionScore> CriterionScores { get; set; } = [];
    public List<CategoryScore> CategoryScores { get; set; } = [];
}

public class CriterionScore
{
    public int AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public int CriterionId { get; set; }
    public Criterion? Criterion { get; set; }
    public CriterionStatus Status { get; set; }
    public int PointsTargeted { get; set; }
    public int PointsAwarded { get; set; }
    public string? Note { get; set; }
}

public class CategoryScore
{
    public int AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public int Points { get; set; }
    public int MaxPoints { get; set; }
}

public class Evidence
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int CriterionId { get; set; }
    public Criterion? Criterion { get; set; }
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public AssessmentStage Stage { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }
    public required string UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

public static class RatingNames
{
    public const string Platinum = "Platinum";
    public const string Gold = "Gold";
    public const string Silver = "Silver";
    public const string Certified = "Certified";
    public const string NotRated = "Not rated";
}

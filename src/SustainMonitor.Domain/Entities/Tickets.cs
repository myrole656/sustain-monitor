namespace SustainMonitor.Domain.Entities;

/// <summary>A CO-ticket: one unit of work that moves one GBI criterion towards its target.</summary>
public class Ticket
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    /// <summary>e.g. KLT-CO-0012</summary>
    public required string Key { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int CriterionId { get; set; }
    public Criterion? Criterion { get; set; }
    public TicketType Type { get; set; } = TicketType.Gap;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public string? AssigneeId { get; set; }
    public required string ReporterId { get; set; }
    public string? ReviewerId { get; set; }
    public DateOnly? DueDate { get; set; }
    public int PointsAtStake { get; set; }
    /// <summary>Points gained on the criterion, set by the Superior when approving (0 to PointsAtStake).</summary>
    public int? PointsAwardedOnClose { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public List<TicketComment> Comments { get; set; } = [];
    public List<TicketHistory> History { get; set; } = [];
    public List<Evidence> Evidence { get; set; } = [];

    public static string FormatKey(string projectCode, int number) => $"{projectCode}-CO-{number:D4}";
}

public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public required string AuthorId { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TicketHistory
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public required string Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public required string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public class AiAnalysis
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public Assessment? Assessment { get; set; }
    public AiAnalysisKind Kind { get; set; }
    public required string Model { get; set; }
    public required string PromptVersion { get; set; }
    public required string InputHash { get; set; }
    public required string OutputJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ReviewedBy { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Type { get; set; }
    public required string PayloadJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public required string Action { get; set; }
    public required string Entity { get; set; }
    public string? EntityId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

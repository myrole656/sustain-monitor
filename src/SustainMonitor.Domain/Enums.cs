namespace SustainMonitor.Domain;

public enum AssessmentStage { SelfCheck, DesignAssessment, CompletionVerification, Renewal }

public enum AssessmentTrigger { Initial, TicketClosed, Manual, Renewal, LegacyImport }

public enum CriterionStatus { NotStarted, InProgress, Achieved, NotApplicable }

public enum TicketType { Gap, Evidence, Corrective, Improvement }

public enum TicketStatus { Open, InProgress, InReview, Done }

public enum TicketPriority { Low, Medium, High, Critical }

public enum CompanyRole { CEO, ProjectManager, Superior }

public enum CompanyStatus { Pending, Verified, Rejected, Suspended }

public enum ECertStatus { Requested, Issued, Declined, Revoked }

public enum AiAnalysisKind { Summary, GapPlan, TicketCheck, Trend }

/// <summary>Identity role names. Company users carry the same role as their <see cref="CompanyRole"/>.</summary>
public static class Roles
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string CEO = "CEO";
    public const string ProjectManager = "ProjectManager";
    public const string Superior = "Superior";

    public static readonly string[] All = [PlatformAdmin, CEO, ProjectManager, Superior];

    public static string For(CompanyRole role) => role switch
    {
        CompanyRole.CEO => CEO,
        CompanyRole.ProjectManager => ProjectManager,
        CompanyRole.Superior => Superior,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}

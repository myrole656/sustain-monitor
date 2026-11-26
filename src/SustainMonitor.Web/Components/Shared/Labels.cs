using SustainMonitor.Domain;

namespace SustainMonitor.Web.Components.Shared;

/// <summary>Human wording for enums shown on screen.</summary>
public static class Labels
{
    public static string Of(TicketStatus s) => s switch
    {
        TicketStatus.Open => "Open",
        TicketStatus.InProgress => "In progress",
        TicketStatus.InReview => "In review",
        TicketStatus.Done => "Done",
        _ => s.ToString(),
    };

    /// <summary>Button text for moving a ticket from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static string Move(TicketStatus from, TicketStatus to) => (from, to) switch
    {
        (TicketStatus.Open, TicketStatus.InProgress) => "Start work",
        (TicketStatus.InProgress, TicketStatus.Open) => "Move back to Open",
        (TicketStatus.InProgress, TicketStatus.InReview) => "Send for review",
        (TicketStatus.InReview, TicketStatus.Done) => "Approve",
        (TicketStatus.InReview, TicketStatus.InProgress) => "Reject",
        _ => Of(to),
    };

    public static string Of(CompanyRole r) => r switch
    {
        CompanyRole.CEO => "CEO",
        CompanyRole.ProjectManager => "Project Manager",
        CompanyRole.Superior => "Superior",
        _ => r.ToString(),
    };

    public static string Of(AssessmentStage s) => s switch
    {
        AssessmentStage.SelfCheck => "Self-check",
        AssessmentStage.DesignAssessment => "Design Assessment (DA)",
        AssessmentStage.CompletionVerification => "Completion & Verification (CVA)",
        AssessmentStage.Renewal => "Renewal",
        _ => s.ToString(),
    };

    public static string Of(AssessmentTrigger t) => t switch
    {
        AssessmentTrigger.Initial => "Baseline",
        AssessmentTrigger.TicketClosed => "Ticket approved",
        AssessmentTrigger.Manual => "Manual",
        AssessmentTrigger.Renewal => "Renewal",
        AssessmentTrigger.LegacyImport => "Imported",
        _ => t.ToString(),
    };

    public static string Of(CriterionStatus s) => s switch
    {
        CriterionStatus.NotStarted => "Not started",
        CriterionStatus.InProgress => "In progress",
        CriterionStatus.Achieved => "Achieved",
        CriterionStatus.NotApplicable => "Not applicable",
        _ => s.ToString(),
    };

    public static string Date(System.DateTime? d) => d is null ? "—" : d.Value.ToLocalTime().ToString("d MMM yyyy");
    public static string When(System.DateTime? d) => d is null ? "—" : d.Value.ToLocalTime().ToString("d MMM yyyy, h:mm tt");
    public static string Date(DateOnly? d) => d is null ? "—" : d.Value.ToString("d MMM yyyy");
}

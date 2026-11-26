using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Security;

namespace SustainMonitor.Domain.Tickets;

public sealed class TicketWorkflowException(string message) : Exception(message);

/// <summary>
/// CO-ticket moves and who may make them:
///   Project Manager: Open → InProgress, InProgress → InReview (needs evidence), InProgress → Open
///   Superior:        InReview → Done (approve, sets points gained), InReview → InProgress (reject)
/// The CEO views only.
/// </summary>
public static class TicketWorkflow
{
    private static readonly Dictionary<(TicketStatus From, TicketStatus To), CompanyRole> Moves = new()
    {
        [(TicketStatus.Open, TicketStatus.InProgress)] = CompanyRole.ProjectManager,
        [(TicketStatus.InProgress, TicketStatus.Open)] = CompanyRole.ProjectManager,
        [(TicketStatus.InProgress, TicketStatus.InReview)] = CompanyRole.ProjectManager,
        [(TicketStatus.InReview, TicketStatus.Done)] = CompanyRole.Superior,
        [(TicketStatus.InReview, TicketStatus.InProgress)] = CompanyRole.Superior,
    };

    public static bool CanMove(TicketStatus from, TicketStatus to, CompanyRole role) =>
        Moves.TryGetValue((from, to), out var who) && who == role;

    /// <summary>Allowed next statuses for this role, for drawing buttons on the board.</summary>
    public static IReadOnlyList<TicketStatus> NextFor(TicketStatus from, CompanyRole role) =>
        Moves.Where(m => m.Key.From == from && m.Value == role).Select(m => m.Key.To).ToList();

    /// <param name="pointsGained">Required when approving: points the criterion gains (0 to PointsAtStake).</param>
    /// <param name="evidenceCount">Number of evidence files on the ticket.</param>
    public static TicketHistory Move(Ticket ticket, TicketStatus to, string userId, CompanyRole role,
        DateTime now, int evidenceCount, int? pointsGained = null, string? note = null)
    {
        if (!Moves.TryGetValue((ticket.Status, to), out var who))
            throw new TicketWorkflowException($"{ticket.Key}: cannot move from {ticket.Status} to {to}.");
        if (who != role)
            throw new TicketWorkflowException($"{ticket.Key}: only the {Label(who)} can move a ticket from {ticket.Status} to {to}.");

        if (to == TicketStatus.InReview && evidenceCount == 0)
            throw new TicketWorkflowException($"{ticket.Key}: upload at least one evidence file before sending for review.");

        if (to == TicketStatus.Done)
        {
            if (!Permissions.CanReviewTicket(role))
                throw new TicketWorkflowException($"{ticket.Key}: only a Superior can approve.");
            if (userId == ticket.AssigneeId || userId == ticket.ReporterId)
                throw new TicketWorkflowException($"{ticket.Key}: you cannot approve a ticket you raised or work on.");
            if (pointsGained is null)
                throw new TicketWorkflowException($"{ticket.Key}: set the points gained before approving.");
            if (pointsGained < 0 || pointsGained > ticket.PointsAtStake)
                throw new TicketWorkflowException($"{ticket.Key}: points gained must be between 0 and {ticket.PointsAtStake}.");
            ticket.PointsAwardedOnClose = pointsGained;
            ticket.ClosedAt = now;
            ticket.ReviewerId = userId;
        }

        if (to == TicketStatus.InProgress && ticket.Status == TicketStatus.InReview)
            ticket.ReviewerId = userId; // rejected by this Superior

        var history = new TicketHistory
        {
            TicketId = ticket.Id,
            Field = nameof(Ticket.Status),
            OldValue = ticket.Status.ToString(),
            NewValue = note is null ? to.ToString() : $"{to}: {note}",
            ChangedBy = userId,
            ChangedAt = now,
        };
        ticket.Status = to;
        ticket.History.Add(history);
        return history;
    }

    private static string Label(CompanyRole r) => r switch
    {
        CompanyRole.ProjectManager => "Project Manager",
        CompanyRole.Superior => "Superior",
        _ => r.ToString(),
    };
}

using SustainMonitor.Domain.Entities;
using SustainMonitor.Domain.Scoring;

namespace SustainMonitor.Domain.Tickets;

/// <summary>Creates CO-tickets raised by a Project Manager and applies approved ones to the score.</summary>
public static class TicketFactory
{
    public static Ticket Create(Project project, GbiTool tool, Assessment current, int criterionId,
        string title, string? description, int pointsAtStake, TicketType type, TicketPriority priority,
        DateOnly? dueDate, string projectManagerId, IEnumerable<Ticket> openTicketsForCriterion, DateTime now)
    {
        var criterion = tool.Categories.SelectMany(c => c.Criteria).SingleOrDefault(c => c.Id == criterionId)
            ?? throw new TicketWorkflowException($"Criterion {criterionId} is not part of {tool.Code}.");
        if (string.IsNullOrWhiteSpace(title))
            throw new TicketWorkflowException("Title is required.");

        var score = current.CriterionScores.SingleOrDefault(s => s.CriterionId == criterionId)
            ?? throw new TicketWorkflowException($"{criterion.Code} is not in the current assessment.");
        if (score.Status == CriterionStatus.NotApplicable)
            throw new TicketWorkflowException($"{criterion.Code} is marked not applicable.");

        var max = VariantResolver.EffectiveMax(criterion, ProjectContext.From(project));
        var alreadyStaked = openTicketsForCriterion.Where(t => t.Status != TicketStatus.Done).Sum(t => t.PointsAtStake);
        var room = max - score.PointsAwarded - alreadyStaked;
        if (pointsAtStake < 1 || pointsAtStake > room)
            throw new TicketWorkflowException(room <= 0
                ? $"{criterion.Code} has no points left to claim (max {max}, awarded {score.PointsAwarded}, on open tickets {alreadyStaked})."
                : $"{criterion.Code}: points at stake must be between 1 and {room}.");

        return new Ticket
        {
            ProjectId = project.Id,
            Key = Ticket.FormatKey(project.Code, project.NextTicketNumber++),
            Title = title.Trim(),
            Description = description,
            CriterionId = criterionId,
            Type = type,
            Priority = priority,
            Status = TicketStatus.Open,
            AssigneeId = projectManagerId,
            ReporterId = projectManagerId,
            DueDate = dueDate,
            PointsAtStake = pointsAtStake,
            CreatedAt = now,
        };
    }

    /// <summary>After a Superior approves: the criterion gains the ticket's points and a new snapshot is produced.</summary>
    public static Assessment ApplyApproved(Ticket ticket, Assessment current, GbiTool tool, ProjectContext ctx)
    {
        if (ticket.Status != TicketStatus.Done || ticket.PointsAwardedOnClose is null)
            throw new TicketWorkflowException($"{ticket.Key} is not approved yet.");
        var score = current.CriterionScores.Single(s => s.CriterionId == ticket.CriterionId);
        return ScoringEngine.Rescore(current, tool, ctx, ticket.CriterionId,
            score.PointsAwarded + ticket.PointsAwardedOnClose.Value, ticket.Id, ticket.ReviewerId!);
    }
}

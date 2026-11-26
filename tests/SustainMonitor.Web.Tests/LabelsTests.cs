using SustainMonitor.Domain;
using SustainMonitor.Web.Components.Shared;
using Xunit;

namespace SustainMonitor.Web.Tests;

public class LabelsTests
{
    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress, "Start work")]
    [InlineData(TicketStatus.InProgress, TicketStatus.InReview, "Send for review")]
    [InlineData(TicketStatus.InProgress, TicketStatus.Open, "Move back to Open")]
    [InlineData(TicketStatus.InReview, TicketStatus.Done, "Approve")]
    [InlineData(TicketStatus.InReview, TicketStatus.InProgress, "Reject")]
    public void Ticket_buttons_use_plain_words(TicketStatus from, TicketStatus to, string expected) =>
        Assert.Equal(expected, Labels.Move(from, to));

    [Fact]
    public void Every_enum_value_has_a_label()
    {
        foreach (var s in Enum.GetValues<TicketStatus>()) Assert.False(string.IsNullOrWhiteSpace(Labels.Of(s)));
        foreach (var r in Enum.GetValues<CompanyRole>()) Assert.False(string.IsNullOrWhiteSpace(Labels.Of(r)));
        foreach (var a in Enum.GetValues<AssessmentStage>()) Assert.False(string.IsNullOrWhiteSpace(Labels.Of(a)));
        foreach (var t in Enum.GetValues<AssessmentTrigger>()) Assert.False(string.IsNullOrWhiteSpace(Labels.Of(t)));
        foreach (var c in Enum.GetValues<CriterionStatus>()) Assert.False(string.IsNullOrWhiteSpace(Labels.Of(c)));
    }

    [Fact]
    public void Missing_dates_show_a_dash()
    {
        Assert.Equal("—", Labels.Date((DateTime?)null));
        Assert.Equal("—", Labels.Date((DateOnly?)null));
        Assert.Equal("—", Labels.When(null));
        Assert.Equal("15 Jan 2027", Labels.Date(new DateOnly(2027, 1, 15)));
    }
}

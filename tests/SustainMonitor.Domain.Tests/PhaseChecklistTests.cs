using Xunit;
using SustainMonitor.Domain.Projects;

namespace SustainMonitor.Domain.Tests;

public class PhaseChecklistTests
{
    [Fact]
    public void Has_five_phases_and_five_execution_modules()
    {
        Assert.Equal(5, Enum.GetValues<ProjectPhase>().Length);
        var modules = PhaseChecklist.Items.Where(i => i.Phase == ProjectPhase.Execution).Select(i => i.Module).Distinct().ToList();
        Assert.Equal(5, modules.Count);
        Assert.DoesNotContain(null, modules);
    }

    [Fact]
    public void Keys_are_unique()
    {
        Assert.Equal(PhaseChecklist.Items.Count, PhaseChecklist.Items.Select(i => i.Key).Distinct().Count());
    }

    [Fact]
    public void Normalize_drops_duplicates_and_rejects_unknown_keys()
    {
        Assert.Single(PhaseChecklist.Normalize(["init.waste_plan", "init.waste_plan", " "]));
        Assert.Throws<ChecklistException>(() => PhaseChecklist.Normalize(["init.waste_plan", "hack"]));
    }

    [Fact]
    public void Progress_and_current_phase_follow_ticked_items()
    {
        var initiation = PhaseChecklist.Items.Where(i => i.Phase == ProjectPhase.Initiation).Select(i => i.Key).ToList();
        Assert.Equal(ProjectPhase.Initiation, PhaseChecklist.CurrentPhase([]));

        var progress = PhaseChecklist.Progress(initiation.Append("plan.greywater"));
        Assert.True(progress[0].Complete);
        Assert.Equal(1, progress[1].Done);
        Assert.Equal(ProjectPhase.Planning, PhaseChecklist.CurrentPhase(initiation));
        Assert.Equal(ProjectPhase.Closing, PhaseChecklist.CurrentPhase(PhaseChecklist.Items.Select(i => i.Key)));
    }
}

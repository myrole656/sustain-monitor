namespace SustainMonitor.Domain.Projects;

/// <summary>The five project lifecycle phases carried over from the original Sustain Monitor wizard.</summary>
public enum ProjectPhase { Initiation, Planning, Execution, Monitoring, Closing }

public sealed record ChecklistItem(ProjectPhase Phase, string? Module, string Key, string Text);

public sealed record PhaseProgress(ProjectPhase Phase, int Done, int Total)
{
    public bool Complete => Total > 0 && Done == Total;
}

public sealed class ChecklistException(string message) : Exception(message);

/// <summary>
/// Lifecycle checklist per project. Items come from the original Laravel/Filament wizard
/// (Initiation → Planning → Execution with five modules → Monitoring → Closing).
/// It tracks site practice alongside the GBI score; it does not award GBI points by itself.
/// </summary>
public static class PhaseChecklist
{
    public static readonly IReadOnlyList<ChecklistItem> Items =
    [
        I(ProjectPhase.Initiation, null, "init.site_selection", "Site selection complies with low-impact development principles."),
        I(ProjectPhase.Initiation, null, "init.energy_preassessment", "Preliminary energy performance pre-assessment completed."),
        I(ProjectPhase.Initiation, null, "init.water_strategy", "Initial water-reduction strategy prepared."),
        I(ProjectPhase.Initiation, null, "init.waste_plan", "Early waste-minimisation plan drafted."),
        I(ProjectPhase.Initiation, null, "init.sustainability_statement", "Sustainability Intent Statement submitted."),
        I(ProjectPhase.Initiation, null, "init.bim_simulation", "Initial site environmental simulation using BIM completed."),
        I(ProjectPhase.Initiation, null, "init.environment_simulations", "Environmental and site simulations conducted (sun, wind, terrain)."),
        I(ProjectPhase.Initiation, null, "init.clash_free_bim", "Clash-free planning validated using BIM."),

        I(ProjectPhase.Planning, null, "plan.energy_modelling", "BIM-based energy modelling completed."),
        I(ProjectPhase.Planning, null, "plan.passive_design", "Passive design strategies incorporated."),
        I(ProjectPhase.Planning, null, "plan.high_eff", "High-efficiency M&E systems selected."),
        I(ProjectPhase.Planning, null, "plan.rain_harvest", "Rainwater harvesting integrated."),
        I(ProjectPhase.Planning, null, "plan.greywater", "Greywater recycling system designed."),
        I(ProjectPhase.Planning, null, "plan.daylight", "Daylighting simulation meets standards."),
        I(ProjectPhase.Planning, null, "plan.ventilation", "Natural ventilation strategy incorporated."),
        I(ProjectPhase.Planning, null, "plan.low_carbon", "Low-carbon materials specified."),
        I(ProjectPhase.Planning, null, "plan.const_waste", "BIM-based quantity optimisation."),
        I(ProjectPhase.Planning, null, "plan.green_land", "Green landscape plan prepared."),
        I(ProjectPhase.Planning, null, "plan.heat_strategy", "Heat island mitigation included."),
        I(ProjectPhase.Planning, null, "plan.thermal_model", "Thermal modelling completed."),
        I(ProjectPhase.Planning, null, "plan.water_eff", "Water efficiency simulation done."),
        I(ProjectPhase.Planning, null, "plan.mats_lifecycle", "Life-cycle assessment (LCA) conducted."),

        I(ProjectPhase.Execution, Env, "exec.env.monitoring", "Dust, noise, and pollution monitoring implemented."),
        I(ProjectPhase.Execution, Env, "exec.env.erosion", "Erosion and sedimentation control measures in place."),
        I(ProjectPhase.Execution, Env, "exec.env.low_impact", "Low-impact design compliance."),
        I(ProjectPhase.Execution, Env, "exec.env.green_erosion", "Green area erosion control."),
        I(ProjectPhase.Execution, Env, "exec.env.flood_zone", "Flood-zone avoidance reviewed."),
        I(ProjectPhase.Execution, Env, "exec.env.transport", "Access to public transport ensured."),
        I(ProjectPhase.Execution, Waste, "exec.waste.tracking", "Real-time waste tracking."),
        I(ProjectPhase.Execution, Waste, "exec.waste.recycled_tracked", "Recycled materials tracked."),
        I(ProjectPhase.Execution, Waste, "exec.waste.green_certified", "Green-certified materials used (e.g. MyHIJAU)."),
        I(ProjectPhase.Execution, Waste, "exec.waste.local_materials", "Recycled/local materials used."),
        I(ProjectPhase.Execution, Waste, "exec.waste.recycle_50", "Achieved 50% recycling."),
        I(ProjectPhase.Execution, Waste, "exec.waste.prefab", "Prefabricated components used."),
        I(ProjectPhase.Execution, Waste, "exec.waste.low_voc", "Low-VOC materials used."),
        I(ProjectPhase.Execution, Energy, "exec.energy.led", "LED lighting installed."),
        I(ProjectPhase.Execution, Energy, "exec.energy.motion", "Motion sensors installed."),
        I(ProjectPhase.Execution, Energy, "exec.energy.hvac", "Efficient HVAC installed."),
        I(ProjectPhase.Execution, Energy, "exec.energy.renewables", "Renewables installed."),
        I(ProjectPhase.Execution, Energy, "exec.energy.ems", "Smart energy management system."),
        I(ProjectPhase.Execution, Energy, "exec.energy.ms1525", "MS 1525 compliance."),
        I(ProjectPhase.Execution, Energy, "exec.energy.bas", "BAS integrated."),
        I(ProjectPhase.Execution, Energy, "exec.energy.envelope", "Efficient building envelope."),
        I(ProjectPhase.Execution, Water, "exec.water.landscape", "Drought-tolerant landscaping."),
        I(ProjectPhase.Execution, Water, "exec.water.low_flow", "Low-flow fixtures installed."),
        I(ProjectPhase.Execution, Water, "exec.water.systems", "Water-efficient systems."),
        I(ProjectPhase.Execution, Water, "exec.water.reduction", "Water reduction achieved."),
        I(ProjectPhase.Execution, Water, "exec.water.monitoring", "Water monitoring installed."),
        I(ProjectPhase.Execution, Water, "exec.water.leak", "Leak detection system."),
        I(ProjectPhase.Execution, Ieq, "exec.ieq.vent_light", "Good ventilation & lighting."),
        I(ProjectPhase.Execution, Ieq, "exec.ieq.standard", "IEQ standards achieved."),

        I(ProjectPhase.Monitoring, null, "mon.smart_energy", "Smart energy meters."),
        I(ProjectPhase.Monitoring, null, "mon.monthly_review", "Monthly reviews."),
        I(ProjectPhase.Monitoring, null, "mon.iaq_sensors", "IAQ sensors monitored."),
        I(ProjectPhase.Monitoring, null, "mon.thermal_comfort", "Thermal comfort maintained."),
        I(ProjectPhase.Monitoring, null, "mon.green_cleaning", "Green cleaning materials."),
        I(ProjectPhase.Monitoring, null, "mon.maintenance", "Sustainability-aligned maintenance."),

        I(ProjectPhase.Closing, null, "close.salvage", "Material salvage inventory."),
        I(ProjectPhase.Closing, null, "close.recyclable", "Recyclable material requirements met."),
        I(ProjectPhase.Closing, null, "close.segregation", "Waste segregation implemented."),
        I(ProjectPhase.Closing, null, "close.audit_docs", "Audit documents prepared."),
        I(ProjectPhase.Closing, null, "close.recycle_analysis", "Recycling performance analysed."),
        I(ProjectPhase.Closing, null, "close.eol_impact", "End-of-life impact simulation."),
    ];

    public const string Env = "Environmental Protection";
    public const string Waste = "Waste & Material Sustainability";
    public const string Energy = "Energy Efficiency";
    public const string Water = "Water Efficiency";
    public const string Ieq = "Indoor Environmental Quality (IEQ)";

    private static readonly HashSet<string> Keys = Items.Select(i => i.Key).ToHashSet();

    public static bool IsKnown(string key) => Keys.Contains(key);

    /// <summary>Distinct, known keys; throws on an unknown key so bad input never reaches the database.</summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string>? keys)
    {
        var list = (keys ?? []).Select(k => k.Trim()).Where(k => k.Length > 0).Distinct().ToList();
        var unknown = list.Where(k => !IsKnown(k)).ToList();
        if (unknown.Count > 0)
            throw new ChecklistException($"Unknown checklist item(s): {string.Join(", ", unknown)}.");
        return list;
    }

    public static IReadOnlyList<PhaseProgress> Progress(IEnumerable<string> doneKeys)
    {
        var done = doneKeys.ToHashSet();
        return Enum.GetValues<ProjectPhase>()
            .Select(p => new PhaseProgress(p,
                Items.Count(i => i.Phase == p && done.Contains(i.Key)),
                Items.Count(i => i.Phase == p)))
            .ToList();
    }

    /// <summary>The first phase not yet complete; Closing when everything is ticked.</summary>
    public static ProjectPhase CurrentPhase(IEnumerable<string> doneKeys) =>
        Progress(doneKeys).FirstOrDefault(p => !p.Complete)?.Phase ?? ProjectPhase.Closing;

    private static ChecklistItem I(ProjectPhase p, string? module, string key, string text) => new(p, module, key, text);
}

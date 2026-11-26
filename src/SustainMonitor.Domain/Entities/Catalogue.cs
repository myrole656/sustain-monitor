namespace SustainMonitor.Domain.Entities;

/// <summary>A GBI rating tool version, e.g. NREB V1.1 or Interiors V1.01.</summary>
public class GbiTool
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public bool IsActive { get; set; } = true;

    public List<Category> Categories { get; set; } = [];
    public List<RatingBand> RatingBands { get; set; } = [];
}

/// <summary>One of the six GBI parts: EE, EQ, SM, MR, WE, IN.</summary>
public class Category
{
    public int Id { get; set; }
    public int ToolId { get; set; }
    public GbiTool? Tool { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int MaxPoints { get; set; }
    public int SortOrder { get; set; }

    public List<Criterion> Criteria { get; set; } = [];
}

/// <summary>A single scored item, e.g. NREB EE5 "Advanced or Improved EE Performance (BEI)".</summary>
public class Criterion
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int MaxPoints { get; set; }
    public string? Description { get; set; }
    public string? ReferenceUrl { get; set; }
    public int SortOrder { get; set; }

    public List<CriterionVariant> Variants { get; set; } = [];
    public List<CriterionLevel> Levels { get; set; } = [];
    public List<EvidenceRequirement> EvidenceRequirements { get; set; } = [];
    public List<SdgMapping> SdgMappings { get; set; } = [];
}

/// <summary>
/// Alternative max points that apply under a condition, e.g. Interiors EE2 is worth 6 points
/// for spaces over 300 m² instead of 12. ConditionKey is evaluated by <see cref="Scoring.VariantResolver"/>.
/// </summary>
public class CriterionVariant
{
    public int Id { get; set; }
    public int CriterionId { get; set; }
    public Criterion? Criterion { get; set; }
    public required string ConditionKey { get; set; }
    public required string Description { get; set; }
    public int MaxPoints { get; set; }
}

/// <summary>What earns a given number of points for a criterion.</summary>
public class CriterionLevel
{
    public int Id { get; set; }
    public int CriterionId { get; set; }
    public Criterion? Criterion { get; set; }
    public int Points { get; set; }
    public required string Requirement { get; set; }
}

public class EvidenceRequirement
{
    public int Id { get; set; }
    public int CriterionId { get; set; }
    public Criterion? Criterion { get; set; }
    public AssessmentStage Stage { get; set; }
    public required string Description { get; set; }
    public string? SignatoryRole { get; set; }
}

public class RatingBand
{
    public int Id { get; set; }
    public int ToolId { get; set; }
    public GbiTool? Tool { get; set; }
    public required string Name { get; set; }
    public int MinPoints { get; set; }
    public int MaxPoints { get; set; }
}

public class SdgMapping
{
    public int CriterionId { get; set; }
    public Criterion? Criterion { get; set; }
    public int SdgNumber { get; set; }
    public decimal Weight { get; set; } = 1m;
}

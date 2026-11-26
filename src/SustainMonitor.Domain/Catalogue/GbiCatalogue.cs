using SustainMonitor.Domain.Entities;

namespace SustainMonitor.Domain.Catalogue;

/// <summary>
/// Official GBI tool definitions used to seed the database.
/// Sources:
///  - NREB: https://www.greenbuildingindex.org/Files/Resources/GBI%20Tools/GBI%20NREB%20Non-Residential%20Existing%20Building%20Tool%20V1.1%20Final.pdf
///  - Interiors: https://www.greenbuildingindex.org/Files/Resources/GBI%20Tools/GBI%20Design%20Reference%20Guide%20-%20Interiors%20V1.01.pdf
/// Every tool totals 100 points; the rating bands are shared.
/// </summary>
public static class GbiCatalogue
{
    public const string NrebCode = "NREB";
    public const string InteriorsCode = "ID";

    /// <summary>Condition key for Interiors EE2: the 6-point version applies above 300 m².</summary>
    public const string GfaAbove300 = "GfaAbove300";

    public static IReadOnlyList<GbiTool> All() => [Nreb(), Interiors()];

    public static GbiTool Nreb()
    {
        const string url = "https://www.greenbuildingindex.org/Files/Resources/GBI%20Tools/GBI%20NREB%20Non-Residential%20Existing%20Building%20Tool%20V1.1%20Final.pdf";
        var tool = new GbiTool { Code = NrebCode, Name = "Non-Residential Existing Building", Version = "1.1" };
        tool.Categories =
        [
            Cat("EE", "Energy Efficiency", 38, 1, url,
                ("EE1", "Minimum EE Performance", 2),
                ("EE2", "Lighting Zoning", 3),
                ("EE3", "Electrical Sub-metering & Tenant Sub-metering", 2),
                ("EE4", "Renewable Energy", 5),
                ("EE5", "Advanced or Improved EE Performance (BEI)", 15),
                ("EE6", "Enhanced Commissioning / Re-Commissioning / Retro Commissioning", 4),
                ("EE7", "On-going Post Occupancy Commissioning", 2),
                ("EE8", "EE Monitoring & Improvement", 2),
                ("EE9", "Sustainable Maintenance", 3)),
            Cat("EQ", "Indoor Environmental Quality", 21, 2, url,
                ("EQ1", "Minimum IAQ Performance", 1),
                ("EQ2", "Environmental Tobacco Smoke (ETS) Control", 1),
                ("EQ3", "Carbon Dioxide Monitoring and Control", 1),
                ("EQ4", "Indoor Air Pollutants", 2),
                ("EQ5", "Mould Prevention", 1),
                ("EQ6", "Thermal Comfort: Controllability of Systems", 2),
                ("EQ7", "Air Change Effectiveness", 1),
                ("EQ8", "Daylighting", 2),
                ("EQ9", "Daylight Glare Control", 1),
                ("EQ10", "Electric Lighting Levels", 1),
                ("EQ11", "High Frequency Ballasts", 1),
                ("EQ12", "External Views", 2),
                ("EQ13", "Internal Noise Levels", 1),
                ("EQ14", "IAQ Before / During Occupancy", 2),
                ("EQ15", "Occupancy Comfort Survey: Verification", 2)),
            Cat("SM", "Sustainable Site Planning & Management", 10, 3, url,
                ("SM1", "GBI Rated Design & Construction", 1),
                ("SM2", "Building Exterior Management", 1),
                ("SM3", "Integrated Pest Management, Erosion Control & Landscape Management", 1),
                ("SM4", "Green Vehicle Priority", 1),
                ("SM5", "Parking Capacity", 1),
                ("SM6", "Greenery & Roof", 4),
                ("SM7", "Building User Manual", 1)),
            Cat("MR", "Materials & Resources", 9, 4, url,
                ("MR1", "Materials Reuse and Selection", 1),
                ("MR2", "Recycled Content Materials", 1),
                ("MR3", "Sustainable Timber", 1),
                ("MR4", "Sustainable Purchasing Policy", 1),
                ("MR5", "Storage, Collection & Disposal of Recyclables", 3),
                ("MR6", "Refrigerants & Clean Agents", 2)),
            Cat("WE", "Water Efficiency", 12, 5, url,
                ("WE1", "Rainwater Harvesting", 3),
                ("WE2", "Water Recycling", 2),
                ("WE3", "Water Efficient Irrigation / Landscaping", 2),
                ("WE4", "Water Efficient Fittings", 3),
                ("WE5", "Metering & Leak Detection System", 2)),
            Cat("IN", "Innovation", 10, 6, url,
                ("IN1", "Innovation & Environmental Initiatives", 9),
                ("IN2", "Green Building Index Facilitator", 1)),
        ];
        tool.RatingBands = StandardBands();
        return tool;
    }

    public static GbiTool Interiors()
    {
        const string url = "https://www.greenbuildingindex.org/Files/Resources/GBI%20Tools/GBI%20Design%20Reference%20Guide%20-%20Interiors%20V1.01.pdf";
        var tool = new GbiTool { Code = InteriorsCode, Name = "Interiors", Version = "1.01" };
        tool.Categories =
        [
            Cat("EE", "Energy Efficiency", 28, 1, url,
                ("EE1", "Engagement of Qualified Professionals", 2),
                ("EE2", "Lighting & Plug Load", 12),
                ("EE3", "Air Conditioning System", 6),
                ("EE4", "Measurement, Verification & EE Performance", 8)),
            Cat("EQ", "Indoor Environmental Quality", 19, 2, url,
                ("EQ1", "Minimum IAQ Performance", 2),
                ("EQ2", "Indoor Air Pollutants", 3),
                ("EQ3", "Thermal Comfort: Design & Controllability", 2),
                ("EQ4", "Carbon Dioxide Monitoring & Control", 2),
                ("EQ5", "Mould Prevention", 2),
                ("EQ6", "Daylighting", 1),
                ("EQ7", "Daylight Glare Control", 1),
                ("EQ8", "Electrical Lighting Levels", 1),
                ("EQ9", "High Frequency Ballasts", 1),
                ("EQ10", "External Views", 1),
                ("EQ11", "Internal Noise Levels", 1),
                ("EQ12", "IAQ Before & During Occupancy", 1),
                ("EQ13", "Post Occupancy Comfort Survey", 1)),
            Cat("SM", "Sustainable Planning & Management", 25, 3, url,
                ("SM1", "Building Selection", 1),
                ("SM2", "Refurbishment of Existing or Abandoned Interior Space", 1),
                ("SM3", "Public Transportation Access", 1),
                ("SM4", "Community Connectivity", 1),
                ("SM5", "Sustainable Space Design", 10),
                ("SM6", "Indoor Greenscape & Indoor Water Feature", 2),
                ("SM7", "Sustainable Construction", 1),
                ("SM8", "Construction Pollution Control Policy", 1),
                ("SM9", "Storage & Collection of Recyclables", 1),
                ("SM10", "Construction Waste Management", 1),
                ("SM11", "Site Safety", 1),
                ("SM12", "Green Procurement & Operation Policy", 2),
                ("SM13", "Sustainable Maintenance & Green Office Guide", 2)),
            Cat("MR", "Materials & Resources", 14, 4, url,
                ("MR1", "Materials Reuse & Selection", 2),
                ("MR2", "Recycled Content Materials", 4),
                ("MR3", "Regional Materials", 1),
                ("MR4", "Rapidly Renewable Materials & Sustainable Timber", 3),
                ("MR5", "Material Manufacture & Ingredients", 2),
                ("MR6", "Refrigerants & Clean Agents", 2)),
            Cat("WE", "Water Efficiency", 4, 5, url,
                ("WE1", "Water Efficient Landscaping & Water Feature", 1),
                ("WE2", "Water Efficient Fittings", 2),
                ("WE3", "Drinking Water Quality", 1)),
            Cat("IN", "Innovation", 10, 6, url,
                ("IN1", "Innovation in Design & Environmental Design Initiatives", 9),
                ("IN2", "GBI Accredited Facilitator", 1)),
        ];

        // EE2: 12 points for spaces of 300 m² or less (lighting + plug load), 6 points above 300 m² (plug load focus).
        var ee2 = tool.Categories[0].Criteria.Single(c => c.Code == "EE2");
        ee2.Variants.Add(new CriterionVariant
        {
            ConditionKey = GfaAbove300,
            Description = "Interior space above 300 m²: plug-load version, max 6 points",
            MaxPoints = 6,
        });

        tool.RatingBands = StandardBands();
        return tool;
    }

    public static List<RatingBand> StandardBands() =>
    [
        new() { Name = RatingNames.Platinum, MinPoints = 86, MaxPoints = 100 },
        new() { Name = RatingNames.Gold, MinPoints = 76, MaxPoints = 85 },
        new() { Name = RatingNames.Silver, MinPoints = 66, MaxPoints = 75 },
        new() { Name = RatingNames.Certified, MinPoints = 50, MaxPoints = 65 },
    ];

    private static Category Cat(string code, string name, int max, int order, string url,
        params (string Code, string Name, int Max)[] criteria)
    {
        var cat = new Category { Code = code, Name = name, MaxPoints = max, SortOrder = order };
        var i = 1;
        foreach (var c in criteria)
        {
            cat.Criteria.Add(new Criterion
            {
                Code = c.Code,
                Name = c.Name,
                MaxPoints = c.Max,
                ReferenceUrl = url,
                SortOrder = i++,
            });
        }
        return cat;
    }
}

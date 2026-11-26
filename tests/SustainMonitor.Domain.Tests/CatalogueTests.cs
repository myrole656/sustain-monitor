using SustainMonitor.Domain.Catalogue;
using SustainMonitor.Domain.Scoring;
using Xunit;

namespace SustainMonitor.Domain.Tests;

public class CatalogueTests
{
    [Fact]
    public void Nreb_matches_official_tool()
    {
        var tool = GbiCatalogue.Nreb();
        Assert.Equal(100, tool.Categories.Sum(c => c.MaxPoints));
        Assert.Equal(44, tool.Categories.Sum(c => c.Criteria.Count));
        foreach (var cat in tool.Categories)
            Assert.Equal(cat.MaxPoints, cat.Criteria.Sum(c => c.MaxPoints));

        var max = tool.Categories.ToDictionary(c => c.Code, c => c.MaxPoints);
        Assert.Equal(38, max["EE"]);
        Assert.Equal(21, max["EQ"]);
        Assert.Equal(10, max["SM"]);
        Assert.Equal(9, max["MR"]);
        Assert.Equal(12, max["WE"]);
        Assert.Equal(10, max["IN"]);
    }

    [Fact]
    public void Interiors_matches_official_tool()
    {
        var tool = GbiCatalogue.Interiors();
        Assert.Equal(100, tool.Categories.Sum(c => c.MaxPoints));
        Assert.Equal(41, tool.Categories.Sum(c => c.Criteria.Count));
        foreach (var cat in tool.Categories)
            Assert.Equal(cat.MaxPoints, cat.Criteria.Sum(c => c.MaxPoints));
    }

    [Fact]
    public void Criterion_codes_are_unique_per_tool()
    {
        foreach (var tool in GbiCatalogue.All())
        {
            var codes = tool.Categories.SelectMany(c => c.Criteria).Select(c => c.Code).ToList();
            Assert.Equal(codes.Count, codes.Distinct().Count());
        }
    }

    [Fact]
    public void Interiors_EE2_drops_to_6_points_above_300_m2()
    {
        var tool = TestCatalogue.WithIds(GbiCatalogue.Interiors());
        var ee2 = TestCatalogue.Find(tool, "EE2");

        Assert.Equal(12, VariantResolver.EffectiveMax(ee2, new ProjectContext(250m)));
        Assert.Equal(12, VariantResolver.EffectiveMax(ee2, new ProjectContext(300m)));
        Assert.Equal(6, VariantResolver.EffectiveMax(ee2, new ProjectContext(301m)));
        Assert.Equal(12, VariantResolver.EffectiveMax(ee2, new ProjectContext(null)));

        var big = ScoringEngine.Evaluate(tool, [], new ProjectContext(500m));
        Assert.Equal(94, big.MaxPoints);
    }
}

using Music.Generator.Agents.Drums;
using Xunit;

namespace Music.Tests.Generator.Agents.DrumsV2;

public class DrumPhrasePlacementPlanTests
{
    [Fact]
    public void IsBarCovered_WhenBarWithinPlacement_ReturnsTrue()
    {
        var plan = new DrumPhrasePlacementPlan();
        plan.Placements.Add(new DrumPhrasePlacement
        {
            PhraseId = "phrase1",
            StartBar = 1,
            BarCount = 2
        });

        bool covered = plan.IsBarCovered(2);

        Assert.True(covered);
    }

    [Fact]
    public void GetPlacementForBar_WhenBarCovered_ReturnsPlacement()
    {
        var plan = new DrumPhrasePlacementPlan();
        var placement = new DrumPhrasePlacement
        {
            PhraseId = "phrase1",
            StartBar = 1,
            BarCount = 2
        };
        plan.Placements.Add(placement);

        var result = plan.GetPlacementForBar(1);

        Assert.Equal(placement, result);
    }
}

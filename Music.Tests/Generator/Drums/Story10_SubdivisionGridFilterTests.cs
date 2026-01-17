// AI: purpose=Unit tests for Story 10 (Subdivision Grid Filter); validates ApplySubdivisionFilter behavior.
// AI: deps=DrumTrackGeneratorNew.ApplySubdivisionFilter (private method tested via reflection); GrooveSubdivisionPolicy.
// AI: coverage=All 5 AllowedSubdivision flags, combinations, None case, null policy, epsilon comparison for triplets.

using FluentAssertions;
using Music.Generator;
using System.Reflection;

namespace Music.Tests.Generator.Drums;

public class Story10_SubdivisionGridFilterTests
{
    private readonly MethodInfo _applySubdivisionFilterMethod;

    public Story10_SubdivisionGridFilterTests()
    {
        // Use reflection to access private ApplySubdivisionFilter method for testing
        _applySubdivisionFilterMethod = typeof(DrumTrackGeneratorNew)
            .GetMethod("ApplySubdivisionFilter", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ApplySubdivisionFilter method not found");
    }

    private List<DrumOnset> ApplySubdivisionFilter(
        List<DrumOnset> onsets,
        GrooveSubdivisionPolicy? policy,
        int beatsPerBar)
    {
        var result = _applySubdivisionFilterMethod.Invoke(null, new object?[] { onsets, policy, beatsPerBar });
        return (List<DrumOnset>)(result ?? new List<DrumOnset>());
    }

    [Fact]
    public void ApplySubdivisionFilter_WithNullOnsets_ReturnsEmptyList()
    {
        // Arrange
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Quarter };

        // Act
        var result = ApplySubdivisionFilter(null!, policy, 4);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplySubdivisionFilter_WithEmptyOnsets_ReturnsEmptyList()
    {
        // Arrange
        var onsets = new List<DrumOnset>();
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Quarter };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplySubdivisionFilter_WithNullPolicy_ReturnsAllOnsets()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),
            new(DrumRole.Snare, 1, 1.5m, 100, 0),
            new(DrumRole.ClosedHat, 1, 1.75m, 100, 0)
        };

        // Act
        var result = ApplySubdivisionFilter(onsets, null, 4);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(onsets);
    }

    [Fact]
    public void ApplySubdivisionFilter_WithNoneFlag_ReturnsEmptyList()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),
            new(DrumRole.Snare, 1, 2.0m, 100, 0)
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.None };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplySubdivisionFilter_QuarterOnly_AllowsQuarterNotesOnly()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),      // Beat 1 - allowed
            new(DrumRole.Snare, 1, 2.0m, 100, 0),     // Beat 2 - allowed
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0), // Beat 1.5 - filtered
            new(DrumRole.Kick, 1, 3.0m, 100, 0),      // Beat 3 - allowed
            new(DrumRole.ClosedHat, 1, 3.25m, 100, 0), // Beat 3.25 - filtered
            new(DrumRole.Snare, 1, 4.0m, 100, 0)      // Beat 4 - allowed
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Quarter };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(4);
        result.Select(o => o.Beat).Should().BeEquivalentTo(new[] { 1.0m, 2.0m, 3.0m, 4.0m });
    }

    [Fact]
    public void ApplySubdivisionFilter_EighthOnly_AllowsEighthNotesOnly()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),      // Beat 1 - allowed
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0), // Beat 1.5 - allowed
            new(DrumRole.Snare, 1, 2.0m, 100, 0),     // Beat 2 - allowed
            new(DrumRole.ClosedHat, 1, 2.25m, 100, 0), // Beat 2.25 - filtered
            new(DrumRole.ClosedHat, 1, 2.5m, 100, 0), // Beat 2.5 - allowed
            new(DrumRole.ClosedHat, 1, 3.0m, 100, 0), // Beat 3 - allowed
            new(DrumRole.ClosedHat, 1, 3.5m, 100, 0), // Beat 3.5 - allowed
            new(DrumRole.Snare, 1, 4.0m, 100, 0)      // Beat 4 - allowed
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Eighth };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(7);
        result.Select(o => o.Beat).Should().BeEquivalentTo(new[] { 1.0m, 1.5m, 2.0m, 2.5m, 3.0m, 3.5m, 4.0m });
    }

    [Fact]
    public void ApplySubdivisionFilter_SixteenthOnly_AllowsSixteenthNotesOnly()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),      // Beat 1 - allowed
            new(DrumRole.ClosedHat, 1, 1.25m, 100, 0), // Beat 1.25 - allowed
            new(DrumRole.ClosedHat, 1, 1.33m, 100, 0), // Beat 1.33 (triplet) - filtered
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0), // Beat 1.5 - allowed
            new(DrumRole.ClosedHat, 1, 1.75m, 100, 0), // Beat 1.75 - allowed
            new(DrumRole.Snare, 1, 2.0m, 100, 0),     // Beat 2 - allowed
            new(DrumRole.ClosedHat, 1, 2.25m, 100, 0), // Beat 2.25 - allowed
            new(DrumRole.ClosedHat, 1, 2.5m, 100, 0), // Beat 2.5 - allowed
            new(DrumRole.ClosedHat, 1, 2.75m, 100, 0)  // Beat 2.75 - allowed
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Sixteenth };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(8);
        result.Select(o => o.Beat).Should().NotContain(1.33m);
    }

    [Fact]
    public void ApplySubdivisionFilter_EighthTripletOnly_AllowsTripletGridOnly()
    {
        // Arrange - triplets: 1.0, 1.333, 1.667, 2.0, 2.333, 2.667, 3.0, etc.
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),        // Beat 1 - allowed
            new(DrumRole.ClosedHat, 1, 1.333m, 100, 0), // Beat 1.333 (1/3) - allowed
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0),   // Beat 1.5 (eighth) - filtered
            new(DrumRole.ClosedHat, 1, 1.667m, 100, 0), // Beat 1.667 (2/3) - allowed
            new(DrumRole.Snare, 1, 2.0m, 100, 0),       // Beat 2 - allowed
            new(DrumRole.ClosedHat, 1, 2.25m, 100, 0),  // Beat 2.25 (sixteenth) - filtered
            new(DrumRole.ClosedHat, 1, 2.333m, 100, 0), // Beat 2.333 (1/3) - allowed
            new(DrumRole.Kick, 1, 3.0m, 100, 0)         // Beat 3 - allowed
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.EighthTriplet };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(6);
        result.Select(o => o.Beat).Should().NotContain(new[] { 1.5m, 2.25m });
    }

    [Fact]
    public void ApplySubdivisionFilter_SixteenthTripletOnly_AllowsFinerTripletGrid()
    {
        // Arrange - sixteenth triplets: 1.0, 1.167, 1.333, 1.5, 1.667, 1.833, 2.0, etc.
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),        // Beat 1 - allowed
            new(DrumRole.ClosedHat, 1, 1.167m, 100, 0), // Beat 1.167 (1/6) - allowed
            new(DrumRole.ClosedHat, 1, 1.25m, 100, 0),  // Beat 1.25 (sixteenth) - filtered
            new(DrumRole.ClosedHat, 1, 1.333m, 100, 0), // Beat 1.333 (2/6) - allowed
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0),   // Beat 1.5 (3/6) - allowed
            new(DrumRole.ClosedHat, 1, 1.667m, 100, 0), // Beat 1.667 (4/6) - allowed
            new(DrumRole.ClosedHat, 1, 1.833m, 100, 0), // Beat 1.833 (5/6) - allowed
            new(DrumRole.Snare, 1, 2.0m, 100, 0)        // Beat 2 - allowed
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.SixteenthTriplet };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(7);
        result.Select(o => o.Beat).Should().NotContain(1.25m);
    }

    [Fact]
    public void ApplySubdivisionFilter_QuarterAndEighth_AllowsBothGrids()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),      // Beat 1 - allowed (quarter)
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0), // Beat 1.5 - allowed (eighth)
            new(DrumRole.Snare, 1, 2.0m, 100, 0),     // Beat 2 - allowed (quarter)
            new(DrumRole.ClosedHat, 1, 2.25m, 100, 0), // Beat 2.25 - filtered (sixteenth)
            new(DrumRole.ClosedHat, 1, 2.5m, 100, 0), // Beat 2.5 - allowed (eighth)
            new(DrumRole.Kick, 1, 3.0m, 100, 0),      // Beat 3 - allowed (quarter)
            new(DrumRole.ClosedHat, 1, 3.5m, 100, 0), // Beat 3.5 - allowed (eighth)
            new(DrumRole.Snare, 1, 4.0m, 100, 0)      // Beat 4 - allowed (quarter)
        };
        var policy = new GrooveSubdivisionPolicy
        {
            AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth
        };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(7);
        result.Select(o => o.Beat).Should().NotContain(2.25m);
    }

    [Fact]
    public void ApplySubdivisionFilter_AllSubdivisions_AllowsAllPositions()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),        // Quarter
            new(DrumRole.ClosedHat, 1, 1.167m, 100, 0), // Sixteenth triplet
            new(DrumRole.ClosedHat, 1, 1.25m, 100, 0),  // Sixteenth
            new(DrumRole.ClosedHat, 1, 1.333m, 100, 0), // Eighth triplet
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0),   // Eighth
            new(DrumRole.ClosedHat, 1, 1.667m, 100, 0), // Eighth triplet
            new(DrumRole.ClosedHat, 1, 1.75m, 100, 0),  // Sixteenth
            new(DrumRole.Snare, 1, 2.0m, 100, 0)        // Quarter
        };
        var policy = new GrooveSubdivisionPolicy
        {
            AllowedSubdivisions = AllowedSubdivision.Quarter |
                                  AllowedSubdivision.Eighth |
                                  AllowedSubdivision.Sixteenth |
                                  AllowedSubdivision.EighthTriplet |
                                  AllowedSubdivision.SixteenthTriplet
        };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(8);
        result.Should().BeEquivalentTo(onsets);
    }

    [Fact]
    public void ApplySubdivisionFilter_WithMultipleBars_FiltersEachBarCorrectly()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            // Bar 1
            new(DrumRole.Kick, 1, 1.0m, 100, 0),      // Allowed
            new(DrumRole.ClosedHat, 1, 1.5m, 100, 0), // Filtered
            new(DrumRole.Snare, 1, 2.0m, 100, 0),     // Allowed
            // Bar 2
            new(DrumRole.Kick, 2, 1.0m, 100, 0),      // Allowed
            new(DrumRole.ClosedHat, 2, 1.25m, 100, 0), // Filtered
            new(DrumRole.Snare, 2, 2.0m, 100, 0)      // Allowed
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Quarter };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(4);
        result.Where(o => o.BarNumber == 1).Should().HaveCount(2);
        result.Where(o => o.BarNumber == 2).Should().HaveCount(2);
    }

    [Fact]
    public void ApplySubdivisionFilter_WithDifferentBeatsPerBar_AdjustsGrid()
    {
        // Arrange - 3/4 time (3 beats per bar)
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),      // Beat 1 - allowed
            new(DrumRole.Snare, 1, 2.0m, 100, 0),     // Beat 2 - allowed
            new(DrumRole.Kick, 1, 3.0m, 100, 0),      // Beat 3 - allowed
            new(DrumRole.ClosedHat, 1, 3.5m, 100, 0), // Beat 3.5 - filtered (would be in next bar)
            new(DrumRole.ClosedHat, 1, 4.0m, 100, 0)  // Beat 4 - filtered (beyond bar end)
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Quarter };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 3);

        // Assert
        result.Should().HaveCount(3);
        result.Select(o => o.Beat).Should().BeEquivalentTo(new[] { 1.0m, 2.0m, 3.0m });
    }

    [Fact]
    public void ApplySubdivisionFilter_EpsilonComparison_HandlesTripletRecurringFractions()
    {
        // Arrange - test that epsilon comparison handles 1/3 = 0.333... correctly
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 100, 0),
            new(DrumRole.ClosedHat, 1, 1.3333333m, 100, 0), // 1/3 with many decimals
            new(DrumRole.ClosedHat, 1, 1.3334m, 100, 0),    // Slightly off but within epsilon
            new(DrumRole.ClosedHat, 1, 1.333m, 100, 0),     // 1/3 with fewer decimals
            new(DrumRole.ClosedHat, 1, 1.6667m, 100, 0),    // 2/3
            new(DrumRole.Snare, 1, 2.0m, 100, 0)
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.EighthTriplet };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert - all triplet positions should be accepted despite decimal precision variations
        result.Should().HaveCount(6);
    }

    [Fact]
    public void ApplySubdivisionFilter_PreservesOnsetProperties()
    {
        // Arrange
        var onsets = new List<DrumOnset>
        {
            new(DrumRole.Kick, 1, 1.0m, 95, 480)
            {
                IsMustHit = true,
                IsNeverRemove = true,
                IsProtected = true
            },
            new(DrumRole.Snare, 1, 2.0m, 110, 960)
            {
                IsProtected = true
            }
        };
        var policy = new GrooveSubdivisionPolicy { AllowedSubdivisions = AllowedSubdivision.Quarter };

        // Act
        var result = ApplySubdivisionFilter(onsets, policy, 4);

        // Assert
        result.Should().HaveCount(2);
        
        var kick = result.First(o => o.Role == DrumRole.Kick);
        kick.Velocity.Should().Be(95);
        kick.TickPosition.Should().Be(480);
        kick.IsMustHit.Should().BeTrue();
        kick.IsNeverRemove.Should().BeTrue();
        kick.IsProtected.Should().BeTrue();

        var snare = result.First(o => o.Role == DrumRole.Snare);
        snare.Velocity.Should().Be(110);
        snare.TickPosition.Should().Be(960);
        snare.IsProtected.Should().BeTrue();
        snare.IsMustHit.Should().BeFalse();
    }
}

using Music.Generator;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Tests for OnsetStrengthClassifier covering all meters and onset types.
/// Verifies deterministic classification and explicit strength overrides.
/// </summary>
public class OnsetStrengthClassifierTests
{
    // ========================================================================
    // AC 1: Classify Downbeat (beat 1 of bar)
    // ========================================================================

    [Theory]
    [InlineData(4)] // 4/4
    [InlineData(3)] // 3/4
    [InlineData(2)] // 2/4
    [InlineData(5)] // 5/4
    [InlineData(6)] // 6/8
    [InlineData(7)] // 7/4
    public void Classify_Beat1_ReturnsDownbeat_AllMeters(int beatsPerBar)
    {
        var result = OnsetStrengthClassifier.Classify(1.0m, beatsPerBar);
        Assert.Equal(OnsetStrength.Downbeat, result);
    }

    [Fact]
    public void Classify_Beat1WithinEpsilon_ReturnsDownbeat()
    {
        var result = OnsetStrengthClassifier.Classify(1.0009m, 4);
        Assert.Equal(OnsetStrength.Downbeat, result);
    }

    // ========================================================================
    // AC 2.1: Classify Backbeat for 4/4 (beats 2 and 4)
    // ========================================================================

    [Fact]
    public void Classify_Beat2In4_4_ReturnsBackbeat()
    {
        var result = OnsetStrengthClassifier.Classify(2.0m, 4);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Fact]
    public void Classify_Beat4In4_4_ReturnsBackbeat()
    {
        var result = OnsetStrengthClassifier.Classify(4.0m, 4);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    // ========================================================================
    // AC 2.2: Classify Backbeat for 3/4 (beat 2)
    // ========================================================================

    [Fact]
    public void Classify_Beat2In3_4_ReturnsBackbeat()
    {
        var result = OnsetStrengthClassifier.Classify(2.0m, 3);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    // ========================================================================
    // AC 2.3: Other meters - deterministic defaults documented
    // ========================================================================

    [Fact]
    public void Classify_Beat2In2_4_ReturnsBackbeat()
    {
        var result = OnsetStrengthClassifier.Classify(2.0m, 2);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Theory]
    [InlineData(2)] // 6/8 beat 2
    [InlineData(4)] // 6/8 beat 4
    public void Classify_Beats2And4In6_8_ReturnsBackbeat(int beat)
    {
        var result = OnsetStrengthClassifier.Classify(beat, 6);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Theory]
    [InlineData(2)] // 5/4 beat 2
    [InlineData(4)] // 5/4 beat 4
    public void Classify_Beats2And4In5_4_ReturnsBackbeat(int beat)
    {
        var result = OnsetStrengthClassifier.Classify(beat, 5);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Theory]
    [InlineData(3)] // 7/4 beat 3
    [InlineData(5)] // 7/4 beat 5
    public void Classify_Beats3And5In7_4_ReturnsBackbeat(int beat)
    {
        var result = OnsetStrengthClassifier.Classify(beat, 7);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    // ========================================================================
    // AC 3: Classify Strong (e.g., beat 3 in 4/4)
    // ========================================================================

    [Fact]
    public void Classify_Beat3In4_4_ReturnsStrong()
    {
        var result = OnsetStrengthClassifier.Classify(3.0m, 4);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    [Fact]
    public void Classify_Beat3In3_4_ReturnsStrong()
    {
        var result = OnsetStrengthClassifier.Classify(3.0m, 3);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    [Theory]
    [InlineData(3)] // 6/8 beat 3
    [InlineData(5)] // 6/8 beat 5
    public void Classify_Beats3And5In6_8_ReturnsStrong(int beat)
    {
        var result = OnsetStrengthClassifier.Classify(beat, 6);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    [Fact]
    public void Classify_Beat3In5_4_ReturnsStrong()
    {
        var result = OnsetStrengthClassifier.Classify(3.0m, 5);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    // ========================================================================
    // AC 4: Classify Offbeat (.5 positions on eighth grid)
    // ========================================================================

    [Theory]
    [InlineData(1.5)]
    [InlineData(2.5)]
    [InlineData(3.5)]
    [InlineData(4.5)]
    public void Classify_HalfBeatPositions_ReturnsOffbeat_4_4(double beat)
    {
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4);
        Assert.Equal(OnsetStrength.Offbeat, result);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(2.5)]
    public void Classify_HalfBeatPositions_ReturnsOffbeat_3_4(double beat)
    {
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 3);
        Assert.Equal(OnsetStrength.Offbeat, result);
    }

    [Fact]
    public void Classify_OffbeatWithinEpsilon_ReturnsOffbeat()
    {
        var result = OnsetStrengthClassifier.Classify(1.5009m, 4);
        Assert.Equal(OnsetStrength.Offbeat, result);
    }

    // ========================================================================
    // AC 5: Classify Pickup (.75 anticipations)
    // ========================================================================

    [Theory]
    [InlineData(1.75)]
    [InlineData(2.75)]
    [InlineData(3.75)]
    [InlineData(4.75)]
    public void Classify_PickupPositions_ReturnsPickup_4_4(double beat)
    {
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    [Theory]
    [InlineData(1.75)]
    [InlineData(2.75)]
    public void Classify_PickupPositions_ReturnsPickup_3_4(double beat)
    {
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 3);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    [Fact]
    public void Classify_LastSixteenthOfBar_ReturnsPickup()
    {
        var result = OnsetStrengthClassifier.Classify(4.75m, 4);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    [Fact]
    public void Classify_PickupWithinEpsilon_ReturnsPickup()
    {
        var result = OnsetStrengthClassifier.Classify(4.7509m, 4);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    // ========================================================================
    // AC 6: Support explicit GrooveOnsetCandidate.Strength override
    // ========================================================================

    [Fact]
    public void Classify_ExplicitStrengthProvided_ReturnsExplicitStrength()
    {
        // Beat 1 would normally be Downbeat, but override to Ghost
        var result = OnsetStrengthClassifier.Classify(1.0m, 4, OnsetStrength.Ghost);
        Assert.Equal(OnsetStrength.Ghost, result);
    }

    [Fact]
    public void Classify_ExplicitBackbeatOverride_ReturnsBackbeat()
    {
        // Beat 1.5 would normally be Offbeat, but override to Backbeat
        var result = OnsetStrengthClassifier.Classify(1.5m, 4, OnsetStrength.Backbeat);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Fact]
    public void Classify_NoExplicitStrength_ComputesFromPosition()
    {
        var result = OnsetStrengthClassifier.Classify(2.0m, 4, null);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    // ========================================================================
    // AC 7: Triplet grid handling (recurring fractions)
    // ========================================================================

    [Theory]
    [InlineData(1.333333)] // Eighth triplet position 1
    [InlineData(1.666667)] // Eighth triplet position 2
    [InlineData(2.333333)] // Eighth triplet position 1
    [InlineData(2.666667)] // Eighth triplet position 2
    public void Classify_TripletPositions_ReturnsDeterministicResult_4_4(double beat)
    {
        // Triplet positions don't match .5 or .75 exactly, so should classify as Strong or fallback
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4);
        Assert.NotEqual(OnsetStrength.Offbeat, result);
        Assert.NotEqual(OnsetStrength.Pickup, result);
    }

    [Theory]
    [InlineData(1.166667)] // Sixteenth triplet
    [InlineData(1.833333)] // Sixteenth triplet
    public void Classify_SixteenthTripletPositions_ReturnsDeterministicResult(double beat)
    {
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4);
        // Should not be classified as offbeat or pickup
        Assert.True(result == OnsetStrength.Strong || result == OnsetStrength.Downbeat || 
                    result == OnsetStrength.Backbeat);
    }

    // ========================================================================
    // Determinism verification
    // ========================================================================

    [Fact]
    public void Classify_SameInputs_ProducesSameOutput()
    {
        var result1 = OnsetStrengthClassifier.Classify(2.5m, 4);
        var result2 = OnsetStrengthClassifier.Classify(2.5m, 4);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Classify_AllCommonBeatsIn4_4_Deterministic()
    {
        // Test all quarter and eighth positions for determinism
        var beats = new[] { 1.0m, 1.5m, 2.0m, 2.5m, 3.0m, 3.5m, 4.0m, 4.5m };
        var expected = new[]
        {
            OnsetStrength.Downbeat,  // 1.0
            OnsetStrength.Offbeat,   // 1.5
            OnsetStrength.Backbeat,  // 2.0
            OnsetStrength.Offbeat,   // 2.5
            OnsetStrength.Strong,    // 3.0
            OnsetStrength.Offbeat,   // 3.5
            OnsetStrength.Backbeat,  // 4.0
            OnsetStrength.Offbeat    // 4.5
        };

        for (int i = 0; i < beats.Length; i++)
        {
            var result = OnsetStrengthClassifier.Classify(beats[i], 4);
            Assert.Equal(expected[i], result);
        }
    }

    [Fact]
    public void Classify_AllCommonBeatsIn3_4_Deterministic()
    {
        var beats = new[] { 1.0m, 1.5m, 2.0m, 2.5m, 3.0m, 3.5m };
        var expected = new[]
        {
            OnsetStrength.Downbeat,  // 1.0
            OnsetStrength.Offbeat,   // 1.5
            OnsetStrength.Backbeat,  // 2.0
            OnsetStrength.Offbeat,   // 2.5
            OnsetStrength.Strong,    // 3.0
            OnsetStrength.Offbeat    // 3.5
        };

        for (int i = 0; i < beats.Length; i++)
        {
            var result = OnsetStrengthClassifier.Classify(beats[i], 3);
            Assert.Equal(expected[i], result);
        }
    }

    // ========================================================================
    // Edge cases
    // ========================================================================

    [Fact]
    public void Classify_ExactIntegerBeat_NoFloatingPointIssues()
    {
        var result = OnsetStrengthClassifier.Classify(2m, 4);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Fact]
    public void Classify_VerySmallEpsilonOffset_CorrectlyClassified()
    {
        var result = OnsetStrengthClassifier.Classify(1.0001m, 4);
        Assert.Equal(OnsetStrength.Downbeat, result);
    }

    [Theory]
    [InlineData(0.25)] // Sixteenth position
    [InlineData(1.25)] // Sixteenth position
    [InlineData(2.25)] // Sixteenth position
    public void Classify_SixteenthPositions_NotOffbeatOrPickup(double beat)
    {
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4);
        Assert.NotEqual(OnsetStrength.Offbeat, result);
        Assert.NotEqual(OnsetStrength.Pickup, result);
    }

    // ========================================================================
    // Fallback behavior for unusual meters
    // ========================================================================

    [Theory]
    [InlineData(9)]  // 9/8
    [InlineData(11)] // 11/8
    [InlineData(13)] // 13/8
    public void Classify_UnusualMeters_UseDeterministicFallback(int beatsPerBar)
    {
        // Beat 1 should always be downbeat
        var result1 = OnsetStrengthClassifier.Classify(1.0m, beatsPerBar);
        Assert.Equal(OnsetStrength.Downbeat, result1);

        // Offbeats should still be detected
        var result2 = OnsetStrengthClassifier.Classify(1.5m, beatsPerBar);
        Assert.Equal(OnsetStrength.Offbeat, result2);

        // Pickups should still be detected
        var result3 = OnsetStrengthClassifier.Classify(1.75m, beatsPerBar);
        Assert.Equal(OnsetStrength.Pickup, result3);
    }
}

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
    [InlineData(12)] // 12/8
    public void Classify_Beat1_ReturnsDownbeat_AllMeters(int beatsPerBar)
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(1.0m, beatsPerBar, grid);
        Assert.Equal(OnsetStrength.Downbeat, result);
    }

    [Fact]
    public void Classify_Beat1WithinEpsilon_ReturnsDownbeat()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(1.0009m, 4, grid);
        Assert.Equal(OnsetStrength.Downbeat, result);
    }

    // ========================================================================
    // AC 2.1: Classify Backbeat for 4/4 (beats 2 and 4)
    // ========================================================================

    [Fact]
    public void Classify_Beat2In4_4_ReturnsBackbeat()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(2.0m, 4, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Fact]
    public void Classify_Beat4In4_4_ReturnsBackbeat()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(4.0m, 4, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    // ========================================================================
    // AC 2.2: Classify Backbeat for 3/4 (beat 2)
    // ========================================================================

    [Fact]
    public void Classify_Beat2In3_4_ReturnsBackbeat()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(2.0m, 3, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    // ========================================================================
    // AC 2.3: Other meters - deterministic defaults documented
    // ========================================================================

    [Fact]
    public void Classify_Beat2In2_4_ReturnsBackbeat()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(2.0m, 2, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Fact]
    public void Classify_Beat4In6_8_ReturnsBackbeat()
    {
        // 6/8 updated to compound meter: backbeat is beat 4 (second big pulse)
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(4.0m, 6, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Theory]
    [InlineData(2)] // 5/4 beat 2
    [InlineData(4)] // 5/4 beat 4
    public void Classify_Beats2And4In5_4_ReturnsBackbeat(int beat)
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(beat, 5, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Theory]
    [InlineData(3)] // 7/4 beat 3
    [InlineData(5)] // 7/4 beat 5
    public void Classify_Beats3And5In7_4_ReturnsBackbeat(int beat)
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(beat, 7, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    // ========================================================================
    // AC 3: Classify Strong (e.g., beat 3 in 4/4)
    // ========================================================================

    [Fact]
    public void Classify_Beat3In4_4_ReturnsStrong()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(3.0m, 4, grid);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    [Fact]
    public void Classify_Beat3In3_4_ReturnsStrong()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(3.0m, 3, grid);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    [Theory]
    [InlineData(3)] // 6/8 beat 3
    [InlineData(6)] // 6/8 beat 6 (updated from 5 per new spec)
    public void Classify_Beats3And6In6_8_ReturnsStrong(int beat)
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(beat, 6, grid);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    [Fact]
    public void Classify_Beat3In5_4_ReturnsStrong()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(3.0m, 5, grid);
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
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4, grid);
        Assert.Equal(OnsetStrength.Offbeat, result);
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(2.5)]
    public void Classify_HalfBeatPositions_ReturnsOffbeat_3_4(double beat)
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 3, grid);
        Assert.Equal(OnsetStrength.Offbeat, result);
    }

    [Fact]
    public void Classify_OffbeatWithinEpsilon_ReturnsOffbeat()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(1.5009m, 4, grid);
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
        var grid = AllowedSubdivision.Sixteenth; // Sixteenth grid for .75 positions
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4, grid);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    [Theory]
    [InlineData(1.75)]
    [InlineData(2.75)]
    public void Classify_PickupPositions_ReturnsPickup_3_4(double beat)
    {
        var grid = AllowedSubdivision.Sixteenth; // Sixteenth grid for .75 positions
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 3, grid);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    [Fact]
    public void Classify_LastSixteenthOfBar_ReturnsPickup()
    {
        var grid = AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(4.75m, 4, grid);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    [Fact]
    public void Classify_PickupWithinEpsilon_ReturnsPickup()
    {
        var grid = AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(4.7509m, 4, grid);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    // ========================================================================
    // AC 6: Support explicit GrooveOnsetCandidate.Strength override
    // ========================================================================

    [Fact]
    public void Classify_ExplicitStrengthProvided_ReturnsExplicitStrength()
    {
        // Beat 1 would normally be Downbeat, but override to Ghost
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(1.0m, 4, grid, OnsetStrength.Ghost);
        Assert.Equal(OnsetStrength.Ghost, result);
    }

    [Fact]
    public void Classify_ExplicitBackbeatOverride_ReturnsBackbeat()
    {
        // Beat 1.5 would normally be Offbeat, but override to Backbeat
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(1.5m, 4, grid, OnsetStrength.Backbeat);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Fact]
    public void Classify_NoExplicitStrength_ComputesFromPosition()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(2.0m, 4, grid, null);
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
        // Triplet positions with eighth grid should return Strong (not matching .5 or .75)
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4, grid);
        Assert.NotEqual(OnsetStrength.Offbeat, result);
        Assert.NotEqual(OnsetStrength.Pickup, result);
    }

    [Theory]
    [InlineData(1.166667)] // Sixteenth triplet
    [InlineData(1.833333)] // Sixteenth triplet
    public void Classify_SixteenthTripletPositions_ReturnsDeterministicResult(double beat)
    {
        var grid = AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4, grid);
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
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result1 = OnsetStrengthClassifier.Classify(2.5m, 4, grid);
        var result2 = OnsetStrengthClassifier.Classify(2.5m, 4, grid);
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Classify_AllCommonBeatsIn4_4_Deterministic()
    {
        // Test all quarter and eighth positions for determinism
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
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
            var result = OnsetStrengthClassifier.Classify(beats[i], 4, grid);
            Assert.Equal(expected[i], result);
        }
    }

    [Fact]
    public void Classify_AllCommonBeatsIn3_4_Deterministic()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
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
            var result = OnsetStrengthClassifier.Classify(beats[i], 3, grid);
            Assert.Equal(expected[i], result);
        }
    }

    // ========================================================================
    // Edge cases
    // ========================================================================

    [Fact]
    public void Classify_ExactIntegerBeat_NoFloatingPointIssues()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(2m, 4, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Fact]
    public void Classify_VerySmallEpsilonOffset_CorrectlyClassified()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(1.0001m, 4, grid);
        Assert.Equal(OnsetStrength.Downbeat, result);
    }

    [Theory]
    [InlineData(0.25)] // Sixteenth position
    [InlineData(1.25)] // Sixteenth position
    [InlineData(2.25)] // Sixteenth position
    public void Classify_SixteenthPositions_NotOffbeatOrPickup(double beat)
    {
        var grid = AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify((decimal)beat, 4, grid);
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
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        
        // Beat 1 should always be downbeat
        var result1 = OnsetStrengthClassifier.Classify(1.0m, beatsPerBar, grid);
        Assert.Equal(OnsetStrength.Downbeat, result1);

        // Offbeats should still be detected
        var result2 = OnsetStrengthClassifier.Classify(1.5m, beatsPerBar, grid);
        Assert.Equal(OnsetStrength.Offbeat, result2);

        // Pickups should still be detected with sixteenth grid
        var gridWithSixteenth = AllowedSubdivision.Sixteenth;
        var result3 = OnsetStrengthClassifier.Classify(1.75m, beatsPerBar, gridWithSixteenth);
        Assert.Equal(OnsetStrength.Pickup, result3);
    }

    // ========================================================================
    // New tests for triplet grid detection (AC 5 + AC 7 from new story)
    // ========================================================================

    [Fact]
    public void Classify_TripletGridOffbeat_ReturnsOffbeat()
    {
        // Middle triplet (1/3 position) with triplet grid should be offbeat
        var grid = AllowedSubdivision.EighthTriplet;
        var result = OnsetStrengthClassifier.Classify(1.333333m, 4, grid);
        Assert.Equal(OnsetStrength.Offbeat, result);
    }

    [Fact]
    public void Classify_TripletGridPickup_ReturnsPickup()
    {
        // Last triplet (2/3 position) with triplet grid should be pickup
        var grid = AllowedSubdivision.EighthTriplet;
        var result = OnsetStrengthClassifier.Classify(1.666667m, 4, grid);
        Assert.Equal(OnsetStrength.Pickup, result);
    }

    [Fact]
    public void Classify_TripletOffbeatMultipleBars_Deterministic()
    {
        var grid = AllowedSubdivision.EighthTriplet;
        var beats = new[] { 1.333333m, 2.333333m, 3.333333m };
        
        foreach (var beat in beats)
        {
            var result = OnsetStrengthClassifier.Classify(beat, 4, grid);
            Assert.Equal(OnsetStrength.Offbeat, result);
        }
    }

    [Fact]
    public void Classify_TripletPickupMultipleBars_Deterministic()
    {
        var grid = AllowedSubdivision.EighthTriplet;
        var beats = new[] { 1.666667m, 2.666667m, 3.666667m };
        
        foreach (var beat in beats)
        {
            var result = OnsetStrengthClassifier.Classify(beat, 4, grid);
            Assert.Equal(OnsetStrength.Pickup, result);
        }
    }

    // ========================================================================
    // New tests for 12/8 meter (AC 2.3 from new story)
    // ========================================================================

    [Fact]
    public void Classify_12_8_Beat1_ReturnsDownbeat()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(1.0m, 12, grid);
        Assert.Equal(OnsetStrength.Downbeat, result);
    }

    [Fact]
    public void Classify_12_8_Beat7_ReturnsBackbeat()
    {
        // 12/8: backbeat is beat 7 (midpoint pulse in compound meter)
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(7.0m, 12, grid);
        Assert.Equal(OnsetStrength.Backbeat, result);
    }

    [Theory]
    [InlineData(4)]  // 12/8 beat 4
    [InlineData(10)] // 12/8 beat 10
    public void Classify_12_8_Beats4And10_ReturnsStrong(int beat)
    {
        // 12/8: strong beats are 4 and 10 (middle of each pulse group)
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        var result = OnsetStrengthClassifier.Classify(beat, 12, grid);
        Assert.Equal(OnsetStrength.Strong, result);
    }

    [Fact]
    public void Classify_12_8_OffbeatsAndPickups_WorkAsExpected()
    {
        var grid = AllowedSubdivision.Eighth | AllowedSubdivision.Sixteenth;
        
        // Offbeat at .5 positions
        var offbeat = OnsetStrengthClassifier.Classify(1.5m, 12, grid);
        Assert.Equal(OnsetStrength.Offbeat, offbeat);
        
        // Pickup at .75 positions (requires sixteenth grid)
        var gridWithSixteenth = AllowedSubdivision.Sixteenth;
        var pickup = OnsetStrengthClassifier.Classify(1.75m, 12, gridWithSixteenth);
        Assert.Equal(OnsetStrength.Pickup, pickup);
    }
}


using Music.Generator;
using Music.Generator.Groove;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Tests for FeelTimingEngine covering all acceptance criteria from Story E1:
/// - AC1-2: Configuration resolution (override vs fallback for feel and swing)
/// - AC3: Straight feel - no shift
/// - AC4: Swing feel - proportional shift based on swing amount
/// - AC5: Shuffle feel - fixed triplet mapping
/// - AC6: TripletFeel - quantize to triplet grid
/// - AC7: AllowedSubdivisions validation
/// - AC8: Unit tests for each feel mode with swing at 0, 0.5, 1.0
/// </summary>
public class FeelTimingEngineTests
{
    // ========================================================================
    // Test Constants
    // ========================================================================

    private const int TicksPerQuarter = 480;
    private const int MaxShiftTicks = TicksPerQuarter / 6; // 80 ticks for full swing

    // ========================================================================
    // Test Fixtures
    // ========================================================================

    private static GrooveSubdivisionPolicy CreatePolicy(
        GrooveFeel feel = GrooveFeel.Straight,
        double swingAmount = 0.0,
        AllowedSubdivision subdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth)
    {
        return new GrooveSubdivisionPolicy
        {
            Feel = feel,
            SwingAmount01 = swingAmount,
            AllowedSubdivisions = subdivisions
        };
    }

    private static SegmentGrooveProfile CreateSegmentProfile(
        GrooveFeel? overrideFeel = null,
        double? overrideSwing = null)
    {
        return new SegmentGrooveProfile
        {
            SegmentId = "Test",
            OverrideFeel = overrideFeel,
            OverrideSwingAmount01 = overrideSwing
        };
    }

    private static GrooveOnset CreateOnset(
        decimal beat,
        string role = "Kick",
        int? existingOffset = null)
    {
        return new GrooveOnset
        {
            Role = role,
            BarNumber = 1,
            Beat = beat,
            TimingOffsetTicks = existingOffset
        };
    }

    private static IReadOnlyList<GrooveOnset> CreateStraightEighths()
    {
        // Simple 4/4 bar: onsets at 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5
        return new[]
        {
            CreateOnset(1.0m),
            CreateOnset(1.5m),
            CreateOnset(2.0m),
            CreateOnset(2.5m),
            CreateOnset(3.0m),
            CreateOnset(3.5m),
            CreateOnset(4.0m),
            CreateOnset(4.5m)
        };
    }

    // ========================================================================
    // AC1: Read feel from OverrideFeel or fallback to Feel
    // ========================================================================

    [Fact]
    public void ResolveFeel_UsesOverride_WhenOverrideProvided()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Straight);
        var segment = CreateSegmentProfile(overrideFeel: GrooveFeel.Swing);

        var result = FeelTimingEngine.ResolveEffectiveFeel(policy, segment);

        Assert.Equal(GrooveFeel.Swing, result);
    }

    [Fact]
    public void ResolveFeel_UsesFallback_WhenOverrideNull()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Shuffle);
        var segment = CreateSegmentProfile(overrideFeel: null);

        var result = FeelTimingEngine.ResolveEffectiveFeel(policy, segment);

        Assert.Equal(GrooveFeel.Shuffle, result);
    }

    [Fact]
    public void ResolveFeel_UsesFallback_WhenSegmentNull()
    {
        var policy = CreatePolicy(feel: GrooveFeel.TripletFeel);

        var result = FeelTimingEngine.ResolveEffectiveFeel(policy, null);

        Assert.Equal(GrooveFeel.TripletFeel, result);
    }

    // ========================================================================
    // AC2: Read swing from OverrideSwingAmount01 or fallback to SwingAmount01
    // ========================================================================

    [Fact]
    public void ResolveSwingAmount_UsesOverride_WhenOverrideProvided()
    {
        var policy = CreatePolicy(swingAmount: 0.3);
        var segment = CreateSegmentProfile(overrideSwing: 0.8);

        var result = FeelTimingEngine.ResolveEffectiveSwingAmount(policy, segment);

        Assert.Equal(0.8, result);
    }

    [Fact]
    public void ResolveSwingAmount_UsesFallback_WhenOverrideNull()
    {
        var policy = CreatePolicy(swingAmount: 0.7);
        var segment = CreateSegmentProfile(overrideSwing: null);

        var result = FeelTimingEngine.ResolveEffectiveSwingAmount(policy, segment);

        Assert.Equal(0.7, result);
    }

    [Fact]
    public void ResolveSwingAmount_UsesFallback_WhenSegmentNull()
    {
        var policy = CreatePolicy(swingAmount: 0.5);

        var result = FeelTimingEngine.ResolveEffectiveSwingAmount(policy, null);

        Assert.Equal(0.5, result);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]  // Clamp negative to 0
    [InlineData(1.5, 1.0)]   // Clamp above 1 to 1
    [InlineData(0.0, 0.0)]   // Boundary: exactly 0
    [InlineData(1.0, 1.0)]   // Boundary: exactly 1
    public void ResolveSwingAmount_ClampsToValidRange(double input, double expected)
    {
        var policy = CreatePolicy(swingAmount: input);

        var result = FeelTimingEngine.ResolveEffectiveSwingAmount(policy, null);

        Assert.Equal(expected, result);
    }

    // ========================================================================
    // AC3: Straight feel - no shift
    // ========================================================================

    [Fact]
    public void Straight_NoShift_WhenSwingZero()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Straight, swingAmount: 0.0);
        var onsets = CreateStraightEighths();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.All(result, o => Assert.True(o.TimingOffsetTicks is null or 0));
    }

    [Fact]
    public void Straight_NoShift_WhenSwingHalf()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Straight, swingAmount: 0.5);
        var onsets = CreateStraightEighths();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.All(result, o => Assert.True(o.TimingOffsetTicks is null or 0));
    }

    [Fact]
    public void Straight_NoShift_WhenSwingOne()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Straight, swingAmount: 1.0);
        var onsets = CreateStraightEighths();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.All(result, o => Assert.True(o.TimingOffsetTicks is null or 0));
    }

    [Fact]
    public void Straight_PreservesExistingTimingOffset()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Straight, swingAmount: 1.0);
        var onsets = new[] { CreateOnset(1.5m, existingOffset: 25) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.Equal(25, result[0].TimingOffsetTicks);
    }

    // ========================================================================
    // AC4: Swing feel - proportional shift
    // ========================================================================

    [Fact]
    public void Swing_NoShift_WhenSwingZero()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 0.0);
        var onsets = CreateStraightEighths();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // Downbeats should never have offset
        Assert.True(result[0].TimingOffsetTicks is null or 0); // Beat 1.0
        Assert.True(result[2].TimingOffsetTicks is null or 0); // Beat 2.0

        // Offbeats with swing=0 should also have no shift
        Assert.True(result[1].TimingOffsetTicks is null or 0); // Beat 1.5
        Assert.True(result[3].TimingOffsetTicks is null or 0); // Beat 2.5
    }

    [Fact]
    public void Swing_PartialShift_WhenSwingHalf()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 0.5);
        var onsets = CreateStraightEighths();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // Downbeats should never shift
        Assert.True(result[0].TimingOffsetTicks is null or 0); // Beat 1.0
        Assert.True(result[2].TimingOffsetTicks is null or 0); // Beat 2.0

        // Offbeats should have half the max shift (80 * 0.5 = 40 ticks)
        int expectedShift = (int)Math.Round(MaxShiftTicks * 0.5, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedShift, result[1].TimingOffsetTicks); // Beat 1.5
        Assert.Equal(expectedShift, result[3].TimingOffsetTicks); // Beat 2.5
    }

    [Fact]
    public void Swing_MaxShift_WhenSwingOne()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 1.0);
        var onsets = CreateStraightEighths();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // Downbeats should never shift
        Assert.True(result[0].TimingOffsetTicks is null or 0); // Beat 1.0
        Assert.True(result[2].TimingOffsetTicks is null or 0); // Beat 2.0

        // Offbeats should have full shift (80 ticks)
        Assert.Equal(MaxShiftTicks, result[1].TimingOffsetTicks); // Beat 1.5
        Assert.Equal(MaxShiftTicks, result[3].TimingOffsetTicks); // Beat 2.5
    }

    [Fact]
    public void Swing_DownbeatsUnaffected()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 1.0);
        var onsets = new[]
        {
            CreateOnset(1.0m),
            CreateOnset(2.0m),
            CreateOnset(3.0m),
            CreateOnset(4.0m)
        };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.All(result, o => Assert.True(o.TimingOffsetTicks is null or 0));
    }

    [Fact]
    public void Swing_SixteenthsUnaffected()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 1.0);
        var onsets = new[]
        {
            CreateOnset(1.0m),
            CreateOnset(1.25m), // 16th
            CreateOnset(1.5m),  // 8th offbeat - should shift
            CreateOnset(1.75m)  // 16th
        };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.True(result[0].TimingOffsetTicks is null or 0); // 1.0 (downbeat)
        Assert.True(result[1].TimingOffsetTicks is null or 0); // 1.25 (16th)
        Assert.Equal(MaxShiftTicks, result[2].TimingOffsetTicks); // 1.5 (8th offbeat)
        Assert.True(result[3].TimingOffsetTicks is null or 0); // 1.75 (16th)
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.25, 20)]   // 80 * 0.25 = 20
    [InlineData(0.5, 40)]    // 80 * 0.5 = 40
    [InlineData(0.75, 60)]   // 80 * 0.75 = 60
    [InlineData(1.0, 80)]    // 80 * 1.0 = 80
    public void Swing_LinearInterpolation_ProducesExpectedOffset(double swingAmount, int expectedOffset)
    {
        var result = FeelTimingEngine.ComputeFeelOffsetTicks(GrooveFeel.Swing, swingAmount);

        Assert.Equal(expectedOffset, result);
    }

    // ========================================================================
    // AC5: Shuffle feel - fixed triplet mapping
    // ========================================================================

    [Fact]
    public void Shuffle_MapsEighthOffbeatToTriplet_WhenSwingZero()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Shuffle, swingAmount: 0.0);
        var onsets = new[] { CreateOnset(1.5m) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // Shuffle always applies full triplet shift regardless of swing amount
        Assert.Equal(MaxShiftTicks, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void Shuffle_MapsEighthOffbeatToTriplet_WhenSwingHalf()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Shuffle, swingAmount: 0.5);
        var onsets = new[] { CreateOnset(1.5m) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // Shuffle ignores swing amount - always full shift
        Assert.Equal(MaxShiftTicks, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void Shuffle_MapsEighthOffbeatToTriplet_WhenSwingOne()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Shuffle, swingAmount: 1.0);
        var onsets = new[] { CreateOnset(1.5m) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.Equal(MaxShiftTicks, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void Shuffle_QuarterNotesUnaffected()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Shuffle, swingAmount: 1.0);
        var onsets = new[]
        {
            CreateOnset(1.0m),
            CreateOnset(2.0m),
            CreateOnset(3.0m),
            CreateOnset(4.0m)
        };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.All(result, o => Assert.True(o.TimingOffsetTicks is null or 0));
    }

    // ========================================================================
    // AC6: TripletFeel - quantize to triplet grid
    // ========================================================================

    [Fact]
    public void TripletFeel_QuantizesEighthsToTripletGrid()
    {
        var policy = CreatePolicy(feel: GrooveFeel.TripletFeel, swingAmount: 0.5);
        var onsets = new[] { CreateOnset(1.5m), CreateOnset(2.5m) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // TripletFeel behaves like Shuffle for E1 scope - ignores swing amount
        Assert.Equal(MaxShiftTicks, result[0].TimingOffsetTicks);
        Assert.Equal(MaxShiftTicks, result[1].TimingOffsetTicks);
    }

    [Fact]
    public void TripletFeel_IgnoresSwingAmount()
    {
        var policy0 = CreatePolicy(feel: GrooveFeel.TripletFeel, swingAmount: 0.0);
        var policy1 = CreatePolicy(feel: GrooveFeel.TripletFeel, swingAmount: 1.0);
        var onsets = new[] { CreateOnset(1.5m) };

        var result0 = FeelTimingEngine.ApplyFeelTiming(onsets, policy0);
        var result1 = FeelTimingEngine.ApplyFeelTiming(onsets, policy1);

        Assert.Equal(result0[0].TimingOffsetTicks, result1[0].TimingOffsetTicks);
    }

    // ========================================================================
    // AC7: AllowedSubdivisions validation
    // ========================================================================

    [Fact]
    public void AllowedSubdivisions_SkipsShift_WhenEighthNotAllowed()
    {
        var policy = CreatePolicy(
            feel: GrooveFeel.Swing,
            swingAmount: 1.0,
            subdivisions: AllowedSubdivision.Quarter); // Eighth NOT allowed
        var onsets = new[] { CreateOnset(1.5m) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // Should not shift because Eighth is not in AllowedSubdivisions
        Assert.True(result[0].TimingOffsetTicks is null or 0);
    }

    [Fact]
    public void AllowedSubdivisions_AppliesShift_WhenEighthAllowed()
    {
        var policy = CreatePolicy(
            feel: GrooveFeel.Swing,
            swingAmount: 1.0,
            subdivisions: AllowedSubdivision.Quarter | AllowedSubdivision.Eighth);
        var onsets = new[] { CreateOnset(1.5m) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.Equal(MaxShiftTicks, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void AllowedSubdivisions_NoneSet_NoShiftApplied()
    {
        var policy = CreatePolicy(
            feel: GrooveFeel.Swing,
            swingAmount: 1.0,
            subdivisions: AllowedSubdivision.None);
        var onsets = CreateStraightEighths();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.All(result, o => Assert.True(o.TimingOffsetTicks is null or 0));
    }

    // ========================================================================
    // Eligibility Tests (IsEighthOffbeat)
    // ========================================================================

    [Theory]
    [InlineData(1.0, false)]  // Downbeat
    [InlineData(1.5, true)]   // Eighth offbeat
    [InlineData(2.0, false)]  // Downbeat
    [InlineData(2.5, true)]   // Eighth offbeat
    [InlineData(1.25, false)] // Sixteenth
    [InlineData(1.75, false)] // Sixteenth
    [InlineData(4.5, true)]   // Eighth offbeat at bar end
    public void IsEighthOffbeat_CorrectlyIdentifiesOffbeats(decimal beat, bool expected)
    {
        var result = FeelTimingEngine.IsEighthOffbeat(beat);

        Assert.Equal(expected, result);
    }


    [Theory]
    [InlineData(1.333, false)]  // First triplet (1 + 1/3)
    [InlineData(1.666, false)]  // Second triplet (1 + 2/3)
    [InlineData(1.49, false)]   // Before offbeat (outside epsilon 0.002)
    [InlineData(1.51, false)]   // After offbeat (outside epsilon 0.002)
    public void IsEighthOffbeat_NonEighthPositions_ReturnsFalse(decimal beat, bool expected)
    {
        var result = FeelTimingEngine.IsEighthOffbeat(beat);

        Assert.Equal(expected, result);
    }


    // ========================================================================
    // Additive Timing Tests
    // ========================================================================

    [Fact]
    public void ApplyFeelTiming_AddsToExistingOffset()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 1.0);
        var onsets = new[] { CreateOnset(1.5m, existingOffset: 10) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        // Should add 80 ticks to existing 10 = 90
        Assert.Equal(10 + MaxShiftTicks, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void ApplyFeelTiming_TreatsNullOffsetAsZero()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 0.5);
        var onsets = new[] { CreateOnset(1.5m, existingOffset: null) };

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        int expectedShift = (int)Math.Round(MaxShiftTicks * 0.5, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedShift, result[0].TimingOffsetTicks);
    }

    // ========================================================================
    // Determinism Tests
    // ========================================================================

    [Fact]
    public void SameInputs_ProduceSameTimingOffsets()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 0.7);
        var onsets = CreateStraightEighths();

        var result1 = FeelTimingEngine.ApplyFeelTiming(onsets, policy);
        var result2 = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.Equal(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.Equal(result1[i].TimingOffsetTicks, result2[i].TimingOffsetTicks);
        }
    }

    [Fact]
    public void DifferentSwingAmounts_ProduceDifferentOffsets()
    {
        var policy05 = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 0.5);
        var policy10 = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 1.0);
        var onsets = new[] { CreateOnset(1.5m) };

        var result05 = FeelTimingEngine.ApplyFeelTiming(onsets, policy05);
        var result10 = FeelTimingEngine.ApplyFeelTiming(onsets, policy10);

        Assert.NotEqual(result05[0].TimingOffsetTicks, result10[0].TimingOffsetTicks);
    }

    // ========================================================================
    // Edge Cases
    // ========================================================================

    [Fact]
    public void ApplyFeelTiming_EmptyList_ReturnsEmpty()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 1.0);
        var onsets = Array.Empty<GrooveOnset>();

        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy);

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyFeelTiming_NullOnsets_ThrowsArgumentNullException()
    {
        var policy = CreatePolicy();

        Assert.Throws<ArgumentNullException>(() =>
            FeelTimingEngine.ApplyFeelTiming(null!, policy));
    }

    [Fact]
    public void ApplyFeelTiming_NullPolicy_ThrowsArgumentNullException()
    {
        var onsets = CreateStraightEighths();

        Assert.Throws<ArgumentNullException>(() =>
            FeelTimingEngine.ApplyFeelTiming(onsets, null!));
    }

    [Fact]
    public void ApplyFeelTiming_PreservesImmutability()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 1.0);
        var original = new[] { CreateOnset(1.5m) };

        var result = FeelTimingEngine.ApplyFeelTiming(original, policy);

        // Original should be unchanged
        Assert.True(original[0].TimingOffsetTicks is null or 0);
        // Result should have the shift
        Assert.Equal(MaxShiftTicks, result[0].TimingOffsetTicks);
    }

    // ========================================================================
    // Diagnostics Tests
    // ========================================================================

    [Fact]
    public void ComputeFeelOffsetWithDiagnostics_ReturnsCompleteInfo()
    {
        var policy = CreatePolicy(feel: GrooveFeel.Swing, swingAmount: 0.5);
        var segment = CreateSegmentProfile(overrideFeel: GrooveFeel.Shuffle);
        var onset = CreateOnset(1.5m, existingOffset: 10);

        var diag = FeelTimingEngine.ComputeFeelOffsetWithDiagnostics(onset, policy, segment);

        Assert.Equal("Kick", diag.Role);
        Assert.Equal(1.5m, diag.Beat);
        Assert.Equal(GrooveFeel.Shuffle, diag.EffectiveFeel); // Override wins
        Assert.Equal(0.5, diag.EffectiveSwingAmount01);
        Assert.True(diag.EighthAllowed);
        Assert.True(diag.IsEligibleOffbeat);
        Assert.Equal(10, diag.ExistingOffsetTicks);
        Assert.Equal(MaxShiftTicks, diag.FeelOffsetTicks); // Shuffle = full shift
        Assert.Equal(10 + MaxShiftTicks, diag.FinalOffsetTicks);
    }
}

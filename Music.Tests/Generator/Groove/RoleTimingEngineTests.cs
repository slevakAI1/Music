using Music.Generator;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Tests for RoleTimingEngine covering all acceptance criteria from Story E2:
/// - AC1: Read RoleTimingFeel[role] from GrooveTimingPolicy
/// - AC2: Convert TimingFeel to base tick offset (Ahead=-10, OnTop=0, Behind=+10, LaidBack=+20)
/// - AC3: Apply RoleTimingBiasTicks[role] from GrooveTimingPolicy
/// - AC4: Respect RoleTimingFeelOverride and RoleTimingBiasTicksOverride from GroovePolicyDecision
/// - AC5: Clamp combined per-role timing by MaxAbsTimingBiasTicks
/// - AC6: Unit tests verify clamping and override precedence
/// </summary>
public class RoleTimingEngineTests
{
    // ========================================================================
    // Test Constants (from E2-1 specification)
    // ========================================================================

    private const int AheadTicks = -10;
    private const int OnTopTicks = 0;
    private const int BehindTicks = 10;
    private const int LaidBackTicks = 20;
    private const int DefaultMaxAbsTicks = 50;

    // ========================================================================
    // Test Fixtures
    // ========================================================================

    private static GrooveTimingPolicy CreateTestTimingPolicy()
    {
        return new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                [GrooveRoles.Kick] = TimingFeel.Behind,
                [GrooveRoles.Snare] = TimingFeel.OnTop,
                [GrooveRoles.ClosedHat] = TimingFeel.Ahead,
                [GrooveRoles.Bass] = TimingFeel.LaidBack
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                [GrooveRoles.Kick] = 5,
                [GrooveRoles.Snare] = 0,
                [GrooveRoles.ClosedHat] = -3,
                [GrooveRoles.Bass] = 10
            },
            MaxAbsTimingBiasTicks = DefaultMaxAbsTicks
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

    private static IReadOnlyList<GrooveOnset> CreateTestOnsets()
    {
        return new[]
        {
            CreateOnset(1.0m, GrooveRoles.Kick),
            CreateOnset(1.5m, GrooveRoles.ClosedHat),
            CreateOnset(2.0m, GrooveRoles.Snare),
            CreateOnset(2.5m, GrooveRoles.ClosedHat),
            CreateOnset(3.0m, GrooveRoles.Kick),
            CreateOnset(4.0m, GrooveRoles.Bass)
        };
    }

    // ========================================================================
    // TimingFeel Mappings (E2-1: Precise tick values)
    // ========================================================================

    [Fact]
    public void MapTimingFeelToTicks_AheadMapsToNegativeTenTicks()
    {
        int result = RoleTimingEngine.MapTimingFeelToTicks(TimingFeel.Ahead);

        Assert.Equal(AheadTicks, result);
    }

    [Fact]
    public void MapTimingFeelToTicks_OnTopMapsToZeroTicks()
    {
        int result = RoleTimingEngine.MapTimingFeelToTicks(TimingFeel.OnTop);

        Assert.Equal(OnTopTicks, result);
    }

    [Fact]
    public void MapTimingFeelToTicks_BehindMapsToPlusTenTicks()
    {
        int result = RoleTimingEngine.MapTimingFeelToTicks(TimingFeel.Behind);

        Assert.Equal(BehindTicks, result);
    }

    [Fact]
    public void MapTimingFeelToTicks_LaidBackMapsToPlusTwentyTicks()
    {
        int result = RoleTimingEngine.MapTimingFeelToTicks(TimingFeel.LaidBack);

        Assert.Equal(LaidBackTicks, result);
    }

    [Theory]
    [InlineData(TimingFeel.Ahead, -10)]
    [InlineData(TimingFeel.OnTop, 0)]
    [InlineData(TimingFeel.Behind, 10)]
    [InlineData(TimingFeel.LaidBack, 20)]
    public void MapTimingFeelToTicks_AllValuesMatchSpecification(TimingFeel feel, int expectedTicks)
    {
        int result = RoleTimingEngine.MapTimingFeelToTicks(feel);

        Assert.Equal(expectedTicks, result);
    }

    // ========================================================================
    // Override Precedence (E2-2: Field-level override precedence)
    // ========================================================================

    [Fact]
    public void ResolveEffectiveFeel_UsesPolicy_WhenNoOverride()
    {
        var policy = CreateTestTimingPolicy();

        var result = RoleTimingEngine.ResolveEffectiveFeel(GrooveRoles.Kick, policy, null);

        Assert.Equal(TimingFeel.Behind, result);
    }

    [Fact]
    public void ResolveEffectiveFeel_UsesOverride_WhenProvided()
    {
        var policy = CreateTestTimingPolicy();
        var decision = new GroovePolicyDecision
        {
            RoleTimingFeelOverride = TimingFeel.Ahead
        };

        var result = RoleTimingEngine.ResolveEffectiveFeel(GrooveRoles.Kick, policy, decision);

        Assert.Equal(TimingFeel.Ahead, result);
    }

    [Fact]
    public void ResolveEffectiveBias_UsesPolicy_WhenNoOverride()
    {
        var policy = CreateTestTimingPolicy();

        int result = RoleTimingEngine.ResolveEffectiveBias(GrooveRoles.Kick, policy, null);

        Assert.Equal(5, result);
    }

    [Fact]
    public void ResolveEffectiveBias_UsesOverride_WhenProvided()
    {
        var policy = CreateTestTimingPolicy();
        var decision = new GroovePolicyDecision
        {
            RoleTimingBiasTicksOverride = 25
        };

        int result = RoleTimingEngine.ResolveEffectiveBias(GrooveRoles.Kick, policy, decision);

        Assert.Equal(25, result);
    }

    [Fact]
    public void ResolveRoleTiming_PartialOverride_FeelOnly()
    {
        // Override feel but not bias
        var policy = CreateTestTimingPolicy();
        var decision = new GroovePolicyDecision
        {
            RoleTimingFeelOverride = TimingFeel.LaidBack
            // No bias override - should use policy (5 for Kick)
        };

        var feel = RoleTimingEngine.ResolveEffectiveFeel(GrooveRoles.Kick, policy, decision);
        int bias = RoleTimingEngine.ResolveEffectiveBias(GrooveRoles.Kick, policy, decision);

        Assert.Equal(TimingFeel.LaidBack, feel);
        Assert.Equal(5, bias); // From policy
    }

    [Fact]
    public void ResolveRoleTiming_PartialOverride_BiasOnly()
    {
        // Override bias but not feel
        var policy = CreateTestTimingPolicy();
        var decision = new GroovePolicyDecision
        {
            RoleTimingBiasTicksOverride = -8
            // No feel override - should use policy (Behind for Kick)
        };

        var feel = RoleTimingEngine.ResolveEffectiveFeel(GrooveRoles.Kick, policy, decision);
        int bias = RoleTimingEngine.ResolveEffectiveBias(GrooveRoles.Kick, policy, decision);

        Assert.Equal(TimingFeel.Behind, feel); // From policy
        Assert.Equal(-8, bias);
    }

    // ========================================================================
    // Unknown Role Fallback (E2-6: Deterministic defaults)
    // ========================================================================

    [Fact]
    public void ResolveEffectiveFeel_UnknownRole_DefaultsToOnTop()
    {
        var policy = CreateTestTimingPolicy();

        var result = RoleTimingEngine.ResolveEffectiveFeel("UnknownRole", policy, null);

        Assert.Equal(TimingFeel.OnTop, result);
    }

    [Fact]
    public void ResolveEffectiveBias_UnknownRole_DefaultsToZero()
    {
        var policy = CreateTestTimingPolicy();

        int result = RoleTimingEngine.ResolveEffectiveBias("UnknownRole", policy, null);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ComputeRoleOffset_UnknownRole_ReturnsZero()
    {
        var policy = CreateTestTimingPolicy();

        int result = RoleTimingEngine.ComputeRoleOffset("UnknownRole", policy, null);

        // OnTop (0) + 0 bias = 0
        Assert.Equal(0, result);
    }

    // ========================================================================
    // Role Offset Computation
    // ========================================================================

    [Fact]
    public void ComputeRoleOffset_CombinesFeelAndBias()
    {
        var policy = CreateTestTimingPolicy();

        // Kick: Behind (+10) + bias (5) = 15
        int result = RoleTimingEngine.ComputeRoleOffset(GrooveRoles.Kick, policy, null);

        Assert.Equal(15, result);
    }

    [Theory]
    [InlineData("Kick", 15)]     // Behind (+10) + 5 = 15
    [InlineData("Snare", 0)]     // OnTop (0) + 0 = 0
    [InlineData("ClosedHat", -13)] // Ahead (-10) + (-3) = -13
    [InlineData("Bass", 30)]     // LaidBack (+20) + 10 = 30
    public void ComputeRoleOffset_VariousRoles_ProducesExpectedOffset(string role, int expectedOffset)
    {
        var policy = CreateTestTimingPolicy();

        int result = RoleTimingEngine.ComputeRoleOffset(role, policy, null);

        Assert.Equal(expectedOffset, result);
    }

    // ========================================================================
    // Additive Semantics (E2-3: E2 adds to E1 offset)
    // ========================================================================

    [Fact]
    public void ApplyRoleTiming_AddsToExistingE1Offset()
    {
        var policy = CreateTestTimingPolicy();
        // E1 applied 40 ticks swing offset
        var onsets = new[] { CreateOnset(1.5m, GrooveRoles.Kick, existingOffset: 40) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        // Kick: Behind (+10) + bias (5) = 15, plus E1 (40) = 55, but clamped to 50
        Assert.Equal(50, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void ApplyRoleTiming_AddsToExistingE1Offset_WhenNoClamping()
    {
        var policy = new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                [GrooveRoles.Snare] = TimingFeel.Behind // +10
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                [GrooveRoles.Snare] = 5
            },
            MaxAbsTimingBiasTicks = 100 // High limit
        };
        var onsets = new[] { CreateOnset(2.0m, GrooveRoles.Snare, existingOffset: 20) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        // Behind (+10) + bias (5) + E1 (20) = 35
        Assert.Equal(35, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void ApplyRoleTiming_TreatsNullExistingOffsetAsZero()
    {
        var policy = CreateTestTimingPolicy();
        var onsets = new[] { CreateOnset(1.0m, GrooveRoles.Kick, existingOffset: null) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        // Behind (+10) + bias (5) = 15
        Assert.Equal(15, result[0].TimingOffsetTicks);
    }

    // ========================================================================
    // Clamping (E2-4: Final offset clamping)
    // ========================================================================

    [Fact]
    public void ApplyRoleTiming_ClampsToMaxAbsTimingBiasTicks()
    {
        var policy = new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                [GrooveRoles.Bass] = TimingFeel.LaidBack // +20
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                [GrooveRoles.Bass] = 40 // +20 + 40 = 60
            },
            MaxAbsTimingBiasTicks = 50
        };
        var onsets = new[] { CreateOnset(4.0m, GrooveRoles.Bass) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        Assert.Equal(50, result[0].TimingOffsetTicks); // Clamped to max
    }

    [Fact]
    public void ApplyRoleTiming_ClampsNegativeToMinusBound()
    {
        var policy = new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                [GrooveRoles.ClosedHat] = TimingFeel.Ahead // -10
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                [GrooveRoles.ClosedHat] = -50 // -10 + (-50) = -60
            },
            MaxAbsTimingBiasTicks = 50
        };
        var onsets = new[] { CreateOnset(1.5m, GrooveRoles.ClosedHat) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        Assert.Equal(-50, result[0].TimingOffsetTicks); // Clamped to -max
    }

    [Fact]
    public void ApplyRoleTiming_LargeE1PlusE2_ClampedCorrectly()
    {
        var policy = new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                [GrooveRoles.Kick] = TimingFeel.Behind // +10
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                [GrooveRoles.Kick] = 30 // +10 + 30 = 40 role offset
            },
            MaxAbsTimingBiasTicks = 50
        };
        // E1 swing offset of 40
        var onsets = new[] { CreateOnset(1.5m, GrooveRoles.Kick, existingOffset: 40) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        // Role offset (40) + E1 (40) = 80, clamped to 50
        Assert.Equal(50, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void ApplyRoleTiming_NoClampingWhenWithinBounds()
    {
        var policy = CreateTestTimingPolicy();
        var onsets = new[] { CreateOnset(1.0m, GrooveRoles.Snare) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        // Snare: OnTop (0) + 0 = 0
        Assert.Equal(0, result[0].TimingOffsetTicks);
    }

    // ========================================================================
    // Edge Cases
    // ========================================================================

    [Fact]
    public void ApplyRoleTiming_NullOnsets_ThrowsArgumentNullException()
    {
        var policy = CreateTestTimingPolicy();

        Assert.Throws<ArgumentNullException>(() =>
            RoleTimingEngine.ApplyRoleTiming(null!, policy, null));
    }

    [Fact]
    public void ApplyRoleTiming_EmptyOnsets_ReturnsEmpty()
    {
        var policy = CreateTestTimingPolicy();
        var onsets = Array.Empty<GrooveOnset>();

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyRoleTiming_NullPolicy_UsesDefaults()
    {
        var onsets = new[] { CreateOnset(1.0m, GrooveRoles.Kick) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, null, null);

        // Default: OnTop (0) + 0 bias = 0
        Assert.Equal(0, result[0].TimingOffsetTicks);
    }

    [Fact]
    public void ApplyRoleTiming_ZeroMaxAbsTicks_NoClampingApplied()
    {
        var policy = new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                [GrooveRoles.Bass] = TimingFeel.LaidBack // +20
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                [GrooveRoles.Bass] = 100 // +20 + 100 = 120
            },
            MaxAbsTimingBiasTicks = 0 // No clamping
        };
        var onsets = new[] { CreateOnset(4.0m, GrooveRoles.Bass) };

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        // No clamping when max is 0
        Assert.Equal(120, result[0].TimingOffsetTicks);
    }

    // ========================================================================
    // Determinism
    // ========================================================================

    [Fact]
    public void ApplyRoleTiming_Deterministic_ForSameInputs()
    {
        var policy = CreateTestTimingPolicy();
        var onsets = CreateTestOnsets();

        var result1 = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);
        var result2 = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        Assert.Equal(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.Equal(result1[i].TimingOffsetTicks, result2[i].TimingOffsetTicks);
            Assert.Equal(result1[i].Beat, result2[i].Beat);
            Assert.Equal(result1[i].Role, result2[i].Role);
        }
    }

    [Fact]
    public void ApplyRoleTiming_Immutable_DoesNotModifyInput()
    {
        var policy = CreateTestTimingPolicy();
        var onsets = new[] { CreateOnset(1.0m, GrooveRoles.Kick) };
        var originalOffset = onsets[0].TimingOffsetTicks;

        var result = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);

        // Input unchanged
        Assert.Equal(originalOffset, onsets[0].TimingOffsetTicks);
        // Output is different reference
        Assert.NotSame(onsets[0], result[0]);
    }

    // ========================================================================
    // Diagnostics (E2-7)
    // ========================================================================

    [Fact]
    public void ComputeRoleTimingWithDiagnostics_ReturnsAllFields()
    {
        var policy = CreateTestTimingPolicy();
        var onset = CreateOnset(1.0m, GrooveRoles.Kick, existingOffset: 20);

        var diagnostics = RoleTimingEngine.ComputeRoleTimingWithDiagnostics(onset, policy, null);

        Assert.Equal(GrooveRoles.Kick, diagnostics.Role);
        Assert.Equal(1.0m, diagnostics.Beat);
        Assert.Equal(TimingFeel.Behind, diagnostics.EffectiveTimingFeel);
        Assert.Equal(5, diagnostics.EffectiveTimingBiasTicks);
        Assert.Equal(10, diagnostics.BaseFeelOffsetTicks); // Behind = +10
        Assert.Equal(15, diagnostics.RoleOffsetTicks); // 10 + 5
        Assert.Equal(20, diagnostics.ExistingOffsetTicks);
        Assert.Equal(35, diagnostics.PreClampCombinedOffset); // 20 + 15
        Assert.Equal(35, diagnostics.FinalOffsetTicks); // No clamping needed
        Assert.False(diagnostics.WasClamped);
    }

    [Fact]
    public void ComputeRoleTimingWithDiagnostics_IndicatesWhenClamped()
    {
        var policy = new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                [GrooveRoles.Bass] = TimingFeel.LaidBack
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                [GrooveRoles.Bass] = 40
            },
            MaxAbsTimingBiasTicks = 50
        };
        var onset = CreateOnset(4.0m, GrooveRoles.Bass, existingOffset: 10);

        var diagnostics = RoleTimingEngine.ComputeRoleTimingWithDiagnostics(onset, policy, null);

        Assert.Equal(60, diagnostics.RoleOffsetTicks); // LaidBack(20) + 40
        Assert.Equal(70, diagnostics.PreClampCombinedOffset); // 10 + 60
        Assert.Equal(50, diagnostics.FinalOffsetTicks); // Clamped
        Assert.True(diagnostics.WasClamped);
    }

    // ========================================================================
    // Test Table from PreAnalysis (specific test cases)
    // ========================================================================

    [Theory]
    [InlineData(TimingFeel.Ahead, 0, 0, -10, false)]   // Ahead with zero bias: -10
    [InlineData(TimingFeel.Behind, 5, 0, 15, false)]   // Behind with +5 bias: +15
    [InlineData(TimingFeel.LaidBack, 40, 0, 50, true)] // LaidBack +40 bias -> 60, clamped to 50
    [InlineData(TimingFeel.Ahead, -5, 40, 25, false)]  // Ahead -5 bias + E1 40: -15 + 40 = 25
    [InlineData(TimingFeel.Behind, 30, 40, 50, true)]  // Behind +30 bias + E1 40: 40+40=80, clamped to 50
    public void TestTableFromPreAnalysis(
        TimingFeel feel, int bias, int e1Offset, int expectedFinal, bool expectedClamped)
    {
        var policy = new GrooveTimingPolicy
        {
            RoleTimingFeel = new Dictionary<string, TimingFeel>
            {
                ["TestRole"] = feel
            },
            RoleTimingBiasTicks = new Dictionary<string, int>
            {
                ["TestRole"] = bias
            },
            MaxAbsTimingBiasTicks = 50
        };
        var onset = CreateOnset(1.0m, "TestRole", existingOffset: e1Offset);

        var diagnostics = RoleTimingEngine.ComputeRoleTimingWithDiagnostics(onset, policy, null);

        Assert.Equal(expectedFinal, diagnostics.FinalOffsetTicks);
        Assert.Equal(expectedClamped, diagnostics.WasClamped);
    }
}

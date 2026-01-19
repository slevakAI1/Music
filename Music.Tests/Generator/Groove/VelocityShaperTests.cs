using Music.Generator;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Tests for VelocityShaper covering all acceptance criteria from Story D2:
/// - AC1: Look up RoleStrengthVelocity[role][strength]
/// - AC2: Use VelocityRule.Typical + AccentBias
/// - AC3: Clamp within VelocityRule.Min/Max
/// - AC4: Ghost uses RoleGhostVelocity when defined
/// - AC5: Apply VelocityBiasOverride from policy decision
/// - AC6: Fallback to sensible defaults when lookups fail
/// </summary>
public class VelocityShaperTests
{
    // ========================================================================
    // Test Fixtures
    // ========================================================================

    private static GrooveAccentPolicy CreateTestAccentPolicy()
    {
        return new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["Kick"] = new()
                {
                    [OnsetStrength.Downbeat] = new VelocityRule { Min = 90, Max = 120, Typical = 105, AccentBias = 0 },
                    [OnsetStrength.Strong] = new VelocityRule { Min = 85, Max = 115, Typical = 100, AccentBias = 0 },
                    [OnsetStrength.Offbeat] = new VelocityRule { Min = 70, Max = 105, Typical = 85, AccentBias = -5 },
                },
                ["Snare"] = new()
                {
                    [OnsetStrength.Backbeat] = new VelocityRule { Min = 95, Max = 127, Typical = 112, AccentBias = 5 },
                    [OnsetStrength.Ghost] = new VelocityRule { Min = 20, Max = 50, Typical = 35, AccentBias = 0 },
                },
                ["ClosedHat"] = new()
                {
                    [OnsetStrength.Strong] = new VelocityRule { Min = 55, Max = 85, Typical = 70, AccentBias = 0 },
                    [OnsetStrength.Offbeat] = new VelocityRule { Min = 45, Max = 75, Typical = 60, AccentBias = -3 },
                }
            },
            RoleGhostVelocity = new Dictionary<string, VelocityRule>
            {
                ["Snare"] = new VelocityRule { Min = 25, Max = 45, Typical = 32, AccentBias = 0 }
            }
        };
    }

    // ========================================================================
    // AC1: Look up RoleStrengthVelocity[role][strength]
    // ========================================================================

    [Fact]
    public void ComputeVelocity_DirectLookup_ReturnsCorrectVelocity()
    {
        var policy = CreateTestAccentPolicy();

        // Kick Downbeat: Typical=105, AccentBias=0
        var velocity = VelocityShaper.ComputeVelocity("Kick", OnsetStrength.Downbeat, policy);

        Assert.Equal(105, velocity);
    }

    [Fact]
    public void ComputeVelocity_DirectLookup_SnareBackbeat_ReturnsCorrectVelocity()
    {
        var policy = CreateTestAccentPolicy();

        // Snare Backbeat: Typical=112, AccentBias=5
        var velocity = VelocityShaper.ComputeVelocity("Snare", OnsetStrength.Backbeat, policy);

        Assert.Equal(117, velocity); // 112 + 5 = 117
    }

    [Theory]
    [InlineData("Kick", OnsetStrength.Downbeat, 105)]
    [InlineData("Kick", OnsetStrength.Strong, 100)]
    [InlineData("Kick", OnsetStrength.Offbeat, 80)] // 85 + (-5) = 80
    [InlineData("Snare", OnsetStrength.Backbeat, 117)] // 112 + 5 = 117
    [InlineData("ClosedHat", OnsetStrength.Strong, 70)]
    [InlineData("ClosedHat", OnsetStrength.Offbeat, 57)] // 60 + (-3) = 57
    public void ComputeVelocity_VariousRoleStrengthCombinations_ReturnsExpected(
        string role, OnsetStrength strength, int expectedVelocity)
    {
        var policy = CreateTestAccentPolicy();

        var velocity = VelocityShaper.ComputeVelocity(role, strength, policy);

        Assert.Equal(expectedVelocity, velocity);
    }

    // ========================================================================
    // AC2: Use VelocityRule.Typical + AccentBias
    // ========================================================================

    [Fact]
    public void ComputeVelocity_AppliesPositiveAccentBias()
    {
        var policy = CreateTestAccentPolicy();

        // Snare Backbeat: Typical=112, AccentBias=+5
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Snare", OnsetStrength.Backbeat, policy);

        Assert.Equal(112, diag.Typical);
        Assert.Equal(5, diag.AccentBias);
        Assert.Equal(117, diag.BaseVelocity);
        Assert.Equal(117, velocity);
    }

    [Fact]
    public void ComputeVelocity_AppliesNegativeAccentBias()
    {
        var policy = CreateTestAccentPolicy();

        // Kick Offbeat: Typical=85, AccentBias=-5
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Kick", OnsetStrength.Offbeat, policy);

        Assert.Equal(85, diag.Typical);
        Assert.Equal(-5, diag.AccentBias);
        Assert.Equal(80, diag.BaseVelocity);
        Assert.Equal(80, velocity);
    }

    // ========================================================================
    // AC3: Clamp within VelocityRule.Min/Max
    // ========================================================================

    [Fact]
    public void ComputeVelocity_ClampsToRuleMax_WhenExceeded()
    {
        var policy = new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["Test"] = new()
                {
                    [OnsetStrength.Downbeat] = new VelocityRule 
                    { 
                        Min = 50, Max = 100, Typical = 110, AccentBias = 10 // Base would be 120
                    }
                }
            }
        };

        var velocity = VelocityShaper.ComputeVelocity("Test", OnsetStrength.Downbeat, policy);

        Assert.Equal(100, velocity); // Clamped to Max
    }

    [Fact]
    public void ComputeVelocity_ClampsToRuleMin_WhenBelowMin()
    {
        var policy = new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["Test"] = new()
                {
                    [OnsetStrength.Offbeat] = new VelocityRule 
                    { 
                        Min = 60, Max = 100, Typical = 50, AccentBias = -10 // Base would be 40
                    }
                }
            }
        };

        var velocity = VelocityShaper.ComputeVelocity("Test", OnsetStrength.Offbeat, policy);

        Assert.Equal(60, velocity); // Clamped to Min
    }

    [Fact]
    public void ComputeVelocity_ClampsToMidiRange_WhenRuleBoundsInvalid()
    {
        var policy = new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["Test"] = new()
                {
                    [OnsetStrength.Downbeat] = new VelocityRule 
                    { 
                        Min = 0, Max = 200, Typical = 150, AccentBias = 0 // Invalid MIDI bounds
                    }
                }
            }
        };

        var velocity = VelocityShaper.ComputeVelocity("Test", OnsetStrength.Downbeat, policy);

        Assert.Equal(127, velocity); // Clamped to MIDI max
    }

    [Fact]
    public void ComputeVelocity_NormalizesMinMax_WhenMinGreaterThanMax()
    {
        var policy = new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["Test"] = new()
                {
                    [OnsetStrength.Downbeat] = new VelocityRule 
                    { 
                        Min = 100, Max = 50, Typical = 75, AccentBias = 0 // Min > Max (misconfigured)
                    }
                }
            }
        };

        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Test", OnsetStrength.Downbeat, policy);

        // Min and Max should be swapped
        Assert.Equal(50, diag.RuleMin);
        Assert.Equal(100, diag.RuleMax);
        Assert.Equal(75, velocity); // Within swapped bounds
    }

    // ========================================================================
    // AC4: Ghost uses RoleGhostVelocity when defined (takes precedence)
    // ========================================================================

    [Fact]
    public void ComputeVelocity_Ghost_UsesRoleGhostVelocity_WhenDefined()
    {
        var policy = CreateTestAccentPolicy();

        // Snare has both RoleGhostVelocity (Typical=32) and RoleStrengthVelocity[Ghost] (Typical=35)
        // RoleGhostVelocity should take precedence
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Snare", OnsetStrength.Ghost, policy);

        Assert.Equal(VelocityRuleSource.RoleGhost, diag.RuleSource);
        Assert.Equal(32, diag.Typical);
        Assert.Equal(0, diag.AccentBias); // Ghost velocities have AccentBias forced to 0
        Assert.Equal(32, velocity);
    }

    [Fact]
    public void ComputeVelocity_Ghost_FallsBackToStrengthTable_WhenNoGhostVelocity()
    {
        var policy = new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["TestRole"] = new()
                {
                    [OnsetStrength.Ghost] = new VelocityRule { Min = 20, Max = 40, Typical = 30, AccentBias = 2 }
                }
            }
            // No RoleGhostVelocity defined
        };

        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "TestRole", OnsetStrength.Ghost, policy);

        Assert.Equal(VelocityRuleSource.RoleStrength, diag.RuleSource);
        Assert.Equal(30, diag.Typical);
        Assert.Equal(2, diag.AccentBias);
        Assert.Equal(32, velocity); // 30 + 2
    }

    // ========================================================================
    // AC5: Apply VelocityBiasOverride from policy decision
    // ========================================================================

    [Fact]
    public void ComputeVelocity_PolicyOverride_AddsPositiveBias()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityBiasOverride = 10 };

        // Kick Downbeat: Typical=105, AccentBias=0, then +10 from override
        var velocity = VelocityShaper.ComputeVelocity("Kick", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal(115, velocity); // 105 + 10 = 115
    }

    [Fact]
    public void ComputeVelocity_PolicyOverride_AddsNegativeBias()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityBiasOverride = -20 };

        // Kick Downbeat: Typical=105, AccentBias=0, then -20 from override
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Kick", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal(-20, diag.PolicyAdditive);
        Assert.Equal(85, diag.PreClampVelocity);
        Assert.Equal(90, velocity); // Clamped to rule Min=90
    }

    [Fact]
    public void ComputeVelocity_PolicyOverride_IsAppliedDeterministically()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityBiasOverride = 5 };

        // Same inputs should produce identical outputs
        var v1 = VelocityShaper.ComputeVelocity("Kick", OnsetStrength.Strong, policy, policyDecision);
        var v2 = VelocityShaper.ComputeVelocity("Kick", OnsetStrength.Strong, policy, policyDecision);

        Assert.Equal(v1, v2);
        Assert.Equal(105, v1); // 100 + 5
    }

    [Fact]
    public void ComputeVelocity_NullPolicyDecision_UsesNoOverride()
    {
        var policy = CreateTestAccentPolicy();

        var velocity = VelocityShaper.ComputeVelocity("Kick", OnsetStrength.Downbeat, policy, null);

        Assert.Equal(105, velocity); // No override applied
    }

    // ========================================================================
    // AC5b: Apply VelocityMultiplierOverride from policy decision (Story D2 Q8)
    // ========================================================================

    [Fact]
    public void ComputeVelocity_MultiplierOverride_AppliesMultiplierGreaterThanOne()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityMultiplierOverride = 1.2 };

        // Kick Downbeat: Typical=105, AccentBias=0, base=105, then *1.2 = 126
        var velocity = VelocityShaper.ComputeVelocity("Kick", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal(120, velocity); // round(105 * 1.2) = 126, clamped to rule Max=120
    }

    [Fact]
    public void ComputeVelocity_MultiplierOverride_AppliesMultiplierLessThanOne()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityMultiplierOverride = 0.8 };

        // Kick Downbeat: Typical=105, AccentBias=0, base=105, then *0.8 = 84
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Kick", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal(0.8, diag.PolicyMultiplier);
        Assert.Equal(84, diag.PreClampVelocity); // round(105 * 0.8) = 84
        Assert.Equal(90, velocity); // Clamped to rule Min=90
    }

    [Fact]
    public void ComputeVelocity_MultiplierAndAdditive_AppliesInCorrectOrder()
    {
        var policy = CreateTestAccentPolicy();
        // Per Q9: biased = round(base * multiplier) + additive
        var policyDecision = new GroovePolicyDecision 
        { 
            VelocityMultiplierOverride = 1.1, 
            VelocityBiasOverride = 5 
        };

        // Kick Downbeat: base=105, round(105 * 1.1) = 116, then +5 = 121
        // But clamped to rule Max=120
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Kick", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal(1.1, diag.PolicyMultiplier);
        Assert.Equal(5, diag.PolicyAdditive);
        Assert.Equal(121, diag.PreClampVelocity); // round(105 * 1.1) + 5 = 116 + 5 = 121
        Assert.Equal(120, velocity); // Clamped to rule Max=120
    }

    [Fact]
    public void ComputeVelocity_MultiplierOverride_RoundsAwayFromZero()
    {
        var policy = new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["Test"] = new()
                {
                    [OnsetStrength.Downbeat] = new VelocityRule 
                    { 
                        Min = 1, Max = 127, Typical = 100, AccentBias = 0 
                    }
                }
            }
        };
        // 100 * 0.995 = 99.5, should round to 100 (away from zero)
        var policyDecision = new GroovePolicyDecision { VelocityMultiplierOverride = 0.995 };

        var velocity = VelocityShaper.ComputeVelocity("Test", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal(100, velocity); // 99.5 rounds to 100 (AwayFromZero)
    }

    [Fact]
    public void ComputeVelocity_MultiplierOverride_NullUsesDefaultOne()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityMultiplierOverride = null, VelocityBiasOverride = 5 };

        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Kick", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal(1.0, diag.PolicyMultiplier); // Default multiplier
        Assert.Equal(110, velocity); // 105 * 1.0 + 5 = 110
    }

    // ========================================================================
    // AC6: Fallback to sensible defaults when lookups fail
    // ========================================================================

    [Fact]
    public void ComputeVelocity_Fallback_RoleMissing_UsesGlobalDefault()
    {
        var policy = CreateTestAccentPolicy();

        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "UnknownRole", OnsetStrength.Downbeat, policy);

        Assert.Equal(VelocityRuleSource.FallbackGlobal, diag.RuleSource);
        Assert.Equal(80, diag.Typical); // Global default
        Assert.Equal(0, diag.AccentBias);
        Assert.Equal(80, velocity);
    }

    [Fact]
    public void ComputeVelocity_Fallback_StrengthMissing_UsesOffbeat()
    {
        var policy = CreateTestAccentPolicy();

        // Kick has Offbeat defined, but not Pickup
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Kick", OnsetStrength.Pickup, policy);

        Assert.Equal(VelocityRuleSource.FallbackRoleOffbeat, diag.RuleSource);
        Assert.Equal(85, diag.Typical); // From Kick.Offbeat
        Assert.Equal(-5, diag.AccentBias);
        Assert.Equal(80, velocity); // 85 + (-5) = 80
    }

    [Fact]
    public void ComputeVelocity_Fallback_OffbeatMissing_UsesFirstAvailable()
    {
        var policy = new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["PartialRole"] = new()
                {
                    // Only Backbeat defined, no Offbeat
                    [OnsetStrength.Backbeat] = new VelocityRule { Min = 80, Max = 120, Typical = 100, AccentBias = 0 }
                }
            }
        };

        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "PartialRole", OnsetStrength.Pickup, policy);

        // Should find Backbeat first in priority order (Downbeat > Backbeat > Strong > Pickup > Offbeat)
        Assert.Equal(VelocityRuleSource.FallbackRoleFirst, diag.RuleSource);
        Assert.Equal(100, velocity);
    }

    [Fact]
    public void ComputeVelocity_NullAccentPolicy_UsesGlobalDefault()
    {
        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "AnyRole", OnsetStrength.Downbeat, null);

        Assert.Equal(VelocityRuleSource.FallbackGlobal, diag.RuleSource);
        Assert.Equal(80, velocity);
    }

    // ========================================================================
    // ResolveVelocityRule Tests
    // ========================================================================

    [Fact]
    public void ResolveVelocityRule_DirectLookup_ReturnsRoleStrengthSource()
    {
        var policy = CreateTestAccentPolicy();

        var (rule, source) = VelocityShaper.ResolveVelocityRule("Kick", OnsetStrength.Downbeat, policy);

        Assert.Equal(VelocityRuleSource.RoleStrength, source);
        Assert.Equal(105, rule.Typical);
    }

    [Fact]
    public void ResolveVelocityRule_Ghost_ReturnsRoleGhostSource()
    {
        var policy = CreateTestAccentPolicy();

        var (rule, source) = VelocityShaper.ResolveVelocityRule("Snare", OnsetStrength.Ghost, policy);

        Assert.Equal(VelocityRuleSource.RoleGhost, source);
        Assert.Equal(32, rule.Typical);
    }

    // ========================================================================
    // ShapeVelocities Batch Tests
    // ========================================================================

    [Fact]
    public void ShapeVelocities_PreservesExistingVelocity()
    {
        var policy = CreateTestAccentPolicy();
        var onsets = new List<GrooveOnset>
        {
            new() { Role = "Kick", BarNumber = 1, Beat = 1.0m, Strength = OnsetStrength.Downbeat, Velocity = 127 }
        };

        var result = VelocityShaper.ShapeVelocities(onsets, policy);

        Assert.Single(result);
        Assert.Equal(127, result[0].Velocity); // Preserved, not overwritten
    }

    [Fact]
    public void ShapeVelocities_ComputesVelocity_WhenNull()
    {
        var policy = CreateTestAccentPolicy();
        var onsets = new List<GrooveOnset>
        {
            new() { Role = "Kick", BarNumber = 1, Beat = 1.0m, Strength = OnsetStrength.Downbeat, Velocity = null }
        };

        var result = VelocityShaper.ShapeVelocities(onsets, policy);

        Assert.Single(result);
        Assert.Equal(105, result[0].Velocity); // Computed from policy
    }

    [Fact]
    public void ShapeVelocities_ReturnsNewImmutableRecords()
    {
        var policy = CreateTestAccentPolicy();
        var originalOnset = new GrooveOnset 
        { 
            Role = "Kick", BarNumber = 1, Beat = 1.0m, Strength = OnsetStrength.Downbeat 
        };
        var onsets = new List<GrooveOnset> { originalOnset };

        var result = VelocityShaper.ShapeVelocities(onsets, policy);

        Assert.Single(result);
        Assert.NotSame(originalOnset, result[0]); // New record
        Assert.Null(originalOnset.Velocity); // Original unchanged
        Assert.Equal(105, result[0].Velocity); // New has velocity
    }

    [Fact]
    public void ShapeVelocities_HandlesMultipleOnsets()
    {
        var policy = CreateTestAccentPolicy();
        var onsets = new List<GrooveOnset>
        {
            new() { Role = "Kick", BarNumber = 1, Beat = 1.0m, Strength = OnsetStrength.Downbeat },
            new() { Role = "Snare", BarNumber = 1, Beat = 2.0m, Strength = OnsetStrength.Backbeat },
            new() { Role = "ClosedHat", BarNumber = 1, Beat = 1.5m, Strength = OnsetStrength.Offbeat }
        };

        var result = VelocityShaper.ShapeVelocities(onsets, policy);

        Assert.Equal(3, result.Count);
        Assert.Equal(105, result[0].Velocity); // Kick Downbeat
        Assert.Equal(117, result[1].Velocity); // Snare Backbeat (112 + 5)
        Assert.Equal(57, result[2].Velocity);  // ClosedHat Offbeat (60 - 3)
    }

    [Fact]
    public void ShapeVelocities_AppliesPolicyOverride_ToAllOnsets()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityBiasOverride = 5 };
        var onsets = new List<GrooveOnset>
        {
            new() { Role = "Kick", BarNumber = 1, Beat = 1.0m, Strength = OnsetStrength.Downbeat },
            new() { Role = "Snare", BarNumber = 1, Beat = 2.0m, Strength = OnsetStrength.Backbeat }
        };

        var result = VelocityShaper.ShapeVelocities(onsets, policy, policyDecision);

        Assert.Equal(110, result[0].Velocity); // 105 + 5
        Assert.Equal(122, result[1].Velocity); // 117 + 5
    }

    [Fact]
    public void ShapeVelocities_DefaultsStrength_WhenNull()
    {
        var policy = CreateTestAccentPolicy();
        var onsets = new List<GrooveOnset>
        {
            new() { Role = "Kick", BarNumber = 1, Beat = 1.0m, Strength = null } // No strength classified
        };

        var result = VelocityShaper.ShapeVelocities(onsets, policy);

        // Should default to Strong when Strength is null
        Assert.Equal(100, result[0].Velocity); // Kick Strong: Typical=100
    }

    // ========================================================================
    // Determinism Tests
    // ========================================================================

    [Fact]
    public void ComputeVelocity_IsDeterministic_SameInputsSameOutput()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityBiasOverride = 3 };

        var results = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(VelocityShaper.ComputeVelocity("Kick", OnsetStrength.Downbeat, policy, policyDecision));
        }

        Assert.All(results, v => Assert.Equal(108, v)); // All identical (105 + 3)
    }

    [Fact]
    public void ShapeVelocities_IsDeterministic_MultipleRuns()
    {
        var policy = CreateTestAccentPolicy();
        var onsets = new List<GrooveOnset>
        {
            new() { Role = "Kick", BarNumber = 1, Beat = 1.0m, Strength = OnsetStrength.Downbeat },
            new() { Role = "Snare", BarNumber = 1, Beat = 2.0m, Strength = OnsetStrength.Ghost },
            new() { Role = "ClosedHat", BarNumber = 1, Beat = 1.5m, Strength = OnsetStrength.Offbeat }
        };

        var run1 = VelocityShaper.ShapeVelocities(onsets, policy);
        var run2 = VelocityShaper.ShapeVelocities(onsets, policy);

        Assert.Equal(run1.Count, run2.Count);
        for (int i = 0; i < run1.Count; i++)
        {
            Assert.Equal(run1[i].Velocity, run2[i].Velocity);
        }
    }

    // ========================================================================
    // Diagnostics Tests
    // ========================================================================

    [Fact]
    public void ComputeVelocityWithDiagnostics_CapturesAllFields()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityBiasOverride = 7 };

        var (velocity, diag) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Kick", OnsetStrength.Downbeat, policy, policyDecision);

        Assert.Equal("Kick", diag.Role);
        Assert.Equal(OnsetStrength.Downbeat, diag.Strength);
        Assert.Equal(VelocityRuleSource.RoleStrength, diag.RuleSource);
        Assert.Equal(105, diag.Typical);
        Assert.Equal(0, diag.AccentBias);
        Assert.Equal(105, diag.BaseVelocity);
        Assert.Equal(1.0, diag.PolicyMultiplier);
        Assert.Equal(7, diag.PolicyAdditive);
        Assert.Equal(112, diag.PreClampVelocity);
        Assert.Equal(90, diag.RuleMin);
        Assert.Equal(120, diag.RuleMax);
        Assert.Equal(112, diag.FinalVelocity);
        Assert.Equal(112, velocity);
    }

    [Fact]
    public void ComputeVelocityWithDiagnostics_MatchesComputeVelocity()
    {
        var policy = CreateTestAccentPolicy();
        var policyDecision = new GroovePolicyDecision { VelocityBiasOverride = -5 };

        var simpleVelocity = VelocityShaper.ComputeVelocity("Snare", OnsetStrength.Backbeat, policy, policyDecision);
        var (diagVelocity, _) = VelocityShaper.ComputeVelocityWithDiagnostics(
            "Snare", OnsetStrength.Backbeat, policy, policyDecision);

        Assert.Equal(simpleVelocity, diagVelocity);
    }
}

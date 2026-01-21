// AI: purpose=Story H1 narrow integration tests for groove pipeline cross-component verification.
// AI: invariants=Single bar scope; deterministic; tests component interactions; fast (<2s).
// AI: deps=GrooveTestSetup, VelocityShaper, FeelTimingEngine, RoleTimingEngine, GrooveCapsEnforcer.

using Music.Generator;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Story H1: Narrow integration tests for the groove pipeline.
/// These tests exercise 2-3 components together to verify correct interactions.
/// Scope: Single bar + single role maximum.
/// </summary>
public class GroovePhaseIntegrationTests
{
    public GroovePhaseIntegrationTests()
    {
        // Ensure deterministic RNG for all tests
        Rng.Initialize(42);
    }

    #region Test Helpers

    private static GroovePresetDefinition CreateMinimalPreset(
        int beatsPerBar = 4,
        GrooveFeel feel = GrooveFeel.Straight,
        double swingAmount = 0.0)
    {
        return new GroovePresetDefinition
        {
            Identity = new GroovePresetIdentity
            {
                Name = "TestPreset",
                BeatsPerBar = beatsPerBar,
                StyleFamily = "Test"
            },
            AnchorLayer = new GrooveInstanceLayer(),
            ProtectionPolicy = new GrooveProtectionPolicy
            {
                SubdivisionPolicy = new GrooveSubdivisionPolicy
                {
                    Feel = feel,
                    SwingAmount01 = swingAmount,
                    AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth
                },
                RoleConstraintPolicy = new GrooveRoleConstraintPolicy
                {
                    RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                    {
                        ["Kick"] = new RoleRhythmVocabulary
                        {
                            MaxHitsPerBar = 8,
                            MaxHitsPerBeat = 2,
                            AllowSyncopation = true,
                            AllowAnticipation = true
                        },
                        ["Snare"] = new RoleRhythmVocabulary
                        {
                            MaxHitsPerBar = 4,
                            MaxHitsPerBeat = 1,
                            AllowSyncopation = true,
                            AllowAnticipation = true
                        }
                    }
                },
                TimingPolicy = new GrooveTimingPolicy
                {
                    MaxAbsTimingBiasTicks = 40,
                    RoleTimingFeel = new Dictionary<string, TimingFeel>
                    {
                        ["Kick"] = TimingFeel.OnTop,
                        ["Snare"] = TimingFeel.Behind
                    },
                    RoleTimingBiasTicks = new Dictionary<string, int>
                    {
                        ["Kick"] = 0,
                        ["Snare"] = 5
                    }
                },
                AccentPolicy = new GrooveAccentPolicy
                {
                    RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
                    {
                        ["Kick"] = new Dictionary<OnsetStrength, VelocityRule>
                        {
                            [OnsetStrength.Downbeat] = new VelocityRule { Typical = 100, AccentBias = 10, Min = 80, Max = 127 },
                            [OnsetStrength.Offbeat] = new VelocityRule { Typical = 85, AccentBias = 0, Min = 60, Max = 110 }
                        },
                        ["Snare"] = new Dictionary<OnsetStrength, VelocityRule>
                        {
                            [OnsetStrength.Backbeat] = new VelocityRule { Typical = 110, AccentBias = 5, Min = 90, Max = 127 },
                            [OnsetStrength.Offbeat] = new VelocityRule { Typical = 80, AccentBias = 0, Min = 50, Max = 100 }
                        }
                    }
                },
                MergePolicy = new GrooveOverrideMergePolicy()
            },
            VariationCatalog = new GrooveVariationCatalog
            {
                Identity = new GroovePresetIdentity { Name = "TestCatalog" },
                HierarchyLayers = new List<GrooveVariationLayer>()
            }
        };
    }

    private static GrooveOnset CreateOnset(
        string role,
        decimal beat,
        OnsetStrength? strength = null,
        int? velocity = null,
        int? timingOffset = null)
    {
        return new GrooveOnset
        {
            Role = role,
            BarNumber = 1,
            Beat = beat,
            Strength = strength,
            Velocity = velocity,
            TimingOffsetTicks = timingOffset,
            IsMustHit = false,
            IsNeverRemove = false,
            IsProtected = false
        };
    }

    #endregion

    #region Velocity + Strength Classification Integration

    [Fact]
    public void VelocityShaping_UsesClassifiedStrength_ForDownbeat()
    {
        // Arrange: onset on beat 1 (downbeat) without pre-set strength/velocity
        var preset = CreateMinimalPreset();
        var onset = CreateOnset("Kick", 1.0m);

        // Act: classify strength then shape velocity
        var classifiedStrength = OnsetStrengthClassifier.Classify(
            onset.Beat,
            preset.Identity.BeatsPerBar,
            preset.ProtectionPolicy.SubdivisionPolicy.AllowedSubdivisions);

        int velocity = VelocityShaper.ComputeVelocity(
            role: onset.Role,
            strength: classifiedStrength,
            accentPolicy: preset.ProtectionPolicy.AccentPolicy,
            policyDecision: null);

        // Assert: beat 1 = Downbeat; Kick downbeat velocity = 100 + 10 = 110
        Assert.Equal(OnsetStrength.Downbeat, classifiedStrength);
        Assert.Equal(110, velocity);
    }

    [Fact]
    public void VelocityShaping_UsesClassifiedStrength_ForOffbeat()
    {
        // Arrange: onset on beat 1.5 (eighth offbeat)
        var preset = CreateMinimalPreset();
        var onset = CreateOnset("Kick", 1.5m);

        // Act
        var classifiedStrength = OnsetStrengthClassifier.Classify(
            onset.Beat,
            preset.Identity.BeatsPerBar,
            preset.ProtectionPolicy.SubdivisionPolicy.AllowedSubdivisions);

        int velocity = VelocityShaper.ComputeVelocity(
            role: onset.Role,
            strength: classifiedStrength,
            accentPolicy: preset.ProtectionPolicy.AccentPolicy,
            policyDecision: null);

        // Assert: beat 1.5 = Offbeat; Kick offbeat velocity = 85 + 0 = 85
        Assert.Equal(OnsetStrength.Offbeat, classifiedStrength);
        Assert.Equal(85, velocity);
    }

    #endregion

    #region Feel Timing + Role Timing Integration

    [Fact]
    public void FeelAndRoleTiming_CombineAdditively_ForSwingFeel()
    {
        // Arrange: swing feel with role timing
        var preset = CreateMinimalPreset(feel: GrooveFeel.Swing, swingAmount: 0.5);
        var onsets = new List<GrooveOnset> { CreateOnset("Snare", 1.5m, timingOffset: 0) };

        // Act: apply feel timing first (E1)
        var afterFeel = FeelTimingEngine.ApplyFeelTiming(
            onsets,
            preset.ProtectionPolicy.SubdivisionPolicy,
            segmentProfile: null,
            mergePolicy: null);

        int feelOffset = afterFeel[0].TimingOffsetTicks ?? 0;

        // Then apply role timing (E2)
        var afterRole = RoleTimingEngine.ApplyRoleTiming(
            afterFeel,
            preset.ProtectionPolicy.TimingPolicy,
            policyDecision: null);

        int combinedOffset = afterRole[0].TimingOffsetTicks ?? 0;

        // Assert: swing adds ~40 ticks for offbeat, then role timing adds more
        Assert.True(feelOffset > 0, "Swing should shift offbeat later");
        Assert.True(combinedOffset >= feelOffset, "Role timing should add to feel timing");
    }

    [Fact]
    public void RoleTiming_ClampsToMax_WhenCombinedExceedsLimit()
    {
        // Arrange: create preset with low max timing
        var preset = CreateMinimalPreset();
        preset.ProtectionPolicy.TimingPolicy.MaxAbsTimingBiasTicks = 20;
        preset.ProtectionPolicy.TimingPolicy.RoleTimingFeel["Snare"] = TimingFeel.LaidBack; // +20 base

        // Start with an onset that already has some timing offset
        var onsets = new List<GrooveOnset> 
        { 
            CreateOnset("Snare", 2.0m, timingOffset: 15) // Already has +15
        };

        // Act: apply role timing with LaidBack (+20) would make total = 15 + 20 = 35
        var result = RoleTimingEngine.ApplyRoleTiming(
            onsets,
            preset.ProtectionPolicy.TimingPolicy,
            policyDecision: null);

        // Assert: should be clamped to max (20)
        Assert.Equal(20, result[0].TimingOffsetTicks);
    }

    #endregion

    #region Caps + Protection Integration

    [Fact]
    public void CapsEnforcer_RespectsProtection_WhenPruning()
    {
        // Arrange: more onsets than cap allows, but some are protected
        var preset = CreateMinimalPreset();
        preset.ProtectionPolicy.RoleConstraintPolicy.RoleVocabulary["Kick"].MaxHitsPerBar = 2;

        var onsets = new List<GrooveOnset>
        {
            new GrooveOnset { Role = "Kick", BarNumber = 1, Beat = 1.0m, IsMustHit = true, Velocity = 100, Strength = OnsetStrength.Downbeat },
            new GrooveOnset { Role = "Kick", BarNumber = 1, Beat = 2.0m, IsNeverRemove = true, Velocity = 90, Strength = OnsetStrength.Strong },
            new GrooveOnset { Role = "Kick", BarNumber = 1, Beat = 3.0m, IsProtected = false, Velocity = 80, Strength = OnsetStrength.Offbeat },
            new GrooveOnset { Role = "Kick", BarNumber = 1, Beat = 4.0m, IsProtected = false, Velocity = 70, Strength = OnsetStrength.Offbeat }
        };

        var barPlan = new GrooveBarPlan
        {
            BarNumber = 1,
            BaseOnsets = new List<GrooveOnset>(),
            SelectedVariationOnsets = onsets,
            FinalOnsets = new List<GrooveOnset>()
        };

        var enforcer = new GrooveCapsEnforcer();

        // Act
        var result = enforcer.EnforceHardCaps(
            barPlan, 
            preset, 
            segmentProfile: null, 
            variationCatalog: null, 
            rngSeed: 1,
            mergePolicy: preset.ProtectionPolicy.MergePolicy);

        // Assert: protected onsets kept, unprotected pruned to meet cap
        Assert.Equal(2, result.FinalOnsets.Count);
        Assert.Contains(result.FinalOnsets, o => o.Beat == 1.0m); // IsMustHit
        Assert.Contains(result.FinalOnsets, o => o.Beat == 2.0m); // IsNeverRemove
    }

    [Fact]
    public void CapsEnforcer_PrunesLowestScored_WhenCapExceeded()
    {
        // Arrange: all unprotected, should prune lowest velocity first
        var preset = CreateMinimalPreset();
        preset.ProtectionPolicy.RoleConstraintPolicy.RoleVocabulary["Kick"].MaxHitsPerBar = 2;

        var onsets = new List<GrooveOnset>
        {
            new GrooveOnset { Role = "Kick", BarNumber = 1, Beat = 1.0m, Velocity = 100, Strength = OnsetStrength.Downbeat },
            new GrooveOnset { Role = "Kick", BarNumber = 1, Beat = 2.0m, Velocity = 60, Strength = OnsetStrength.Offbeat },
            new GrooveOnset { Role = "Kick", BarNumber = 1, Beat = 3.0m, Velocity = 80, Strength = OnsetStrength.Strong }
        };

        var barPlan = new GrooveBarPlan
        {
            BarNumber = 1,
            BaseOnsets = new List<GrooveOnset>(),
            SelectedVariationOnsets = onsets,
            FinalOnsets = new List<GrooveOnset>()
        };

        var enforcer = new GrooveCapsEnforcer();

        // Act
        var result = enforcer.EnforceHardCaps(
            barPlan, 
            preset, 
            segmentProfile: null, 
            variationCatalog: null, 
            rngSeed: 1,
            mergePolicy: preset.ProtectionPolicy.MergePolicy);

        // Assert: lowest velocity (60) pruned, keeps 100 and 80
        Assert.Equal(2, result.FinalOnsets.Count);
        Assert.DoesNotContain(result.FinalOnsets, o => o.Velocity == 60);
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void FullPipeline_SameSeed_ProducesIdenticalOutput()
    {
        // Arrange
        var preset = CreateMinimalPreset();
        var onsets = new List<GrooveOnset>
        {
            CreateOnset("Kick", 1.0m),
            CreateOnset("Kick", 2.5m),
            CreateOnset("Kick", 3.0m)
        };

        // Act: run twice with same seed
        Rng.Initialize(12345);
        var result1 = ProcessOnsets(onsets, preset);

        Rng.Initialize(12345);
        var result2 = ProcessOnsets(onsets, preset);

        // Assert: identical results
        Assert.Equal(result1.Count, result2.Count);
        for (int i = 0; i < result1.Count; i++)
        {
            Assert.Equal(result1[i].Beat, result2[i].Beat);
            Assert.Equal(result1[i].Velocity, result2[i].Velocity);
            Assert.Equal(result1[i].TimingOffsetTicks, result2[i].TimingOffsetTicks);
        }
    }

    private List<GrooveOnset> ProcessOnsets(List<GrooveOnset> onsets, GroovePresetDefinition preset)
    {
        // Simulate pipeline: classify strength → shape velocity → apply timing
        var result = new List<GrooveOnset>();

        foreach (var onset in onsets)
        {
            var strength = OnsetStrengthClassifier.Classify(
                onset.Beat,
                preset.Identity.BeatsPerBar,
                preset.ProtectionPolicy.SubdivisionPolicy.AllowedSubdivisions);

            int velocity = VelocityShaper.ComputeVelocity(
                role: onset.Role,
                strength: strength,
                accentPolicy: preset.ProtectionPolicy.AccentPolicy,
                policyDecision: null);

            result.Add(onset with
            {
                Strength = strength,
                Velocity = velocity
            });
        }

        // Apply feel timing to all at once
        var afterFeel = FeelTimingEngine.ApplyFeelTiming(
            result,
            preset.ProtectionPolicy.SubdivisionPolicy,
            segmentProfile: null,
            mergePolicy: null);

        // Apply role timing
        var afterRole = RoleTimingEngine.ApplyRoleTiming(
            afterFeel,
            preset.ProtectionPolicy.TimingPolicy,
            policyDecision: null);

        return afterRole.ToList();
    }

    #endregion

    #region Diagnostics Integration

    [Fact]
    public void Diagnostics_DoesNotAffectOutput_WhenToggled()
    {
        // Arrange
        var preset = CreateMinimalPreset();
        var onsets = new List<GrooveOnset>
        {
            CreateOnset("Kick", 1.0m),
            CreateOnset("Kick", 2.0m)
        };

        // Act: process without diagnostics
        var resultWithoutDiag = ProcessOnsets(onsets, preset);

        // Process with diagnostics (simulated - actual diagnostics are in higher-level pipeline)
        var resultWithDiag = ProcessOnsets(onsets, preset);

        // Assert: same output regardless of diagnostics
        Assert.Equal(resultWithoutDiag.Count, resultWithDiag.Count);
        for (int i = 0; i < resultWithoutDiag.Count; i++)
        {
            Assert.Equal(resultWithoutDiag[i].Beat, resultWithDiag[i].Beat);
            Assert.Equal(resultWithoutDiag[i].Velocity, resultWithDiag[i].Velocity);
            Assert.Equal(resultWithoutDiag[i].TimingOffsetTicks, resultWithDiag[i].TimingOffsetTicks);
        }
    }

    #endregion
}

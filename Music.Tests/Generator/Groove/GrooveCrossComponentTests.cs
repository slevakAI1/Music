// AI: purpose=Story H1 cross-component tests verifying pipeline ordering and interactions.
// AI: invariants=Deterministic; tests component boundaries; fast.
// AI: deps=VelocityShaper, FeelTimingEngine, RoleTimingEngine, OnsetStrengthClassifier.

using Music.Generator;
using Music.Tests.TestFixtures;
using Xunit;

namespace Music.Tests.Generator.Groove;

/// <summary>
/// Story H1: Cross-component tests verifying correct interaction between
/// groove system components. Tests pipeline ordering and data flow.
/// </summary>
public class GrooveCrossComponentTests
{
    public GrooveCrossComponentTests()
    {
        Rng.Initialize(42);
    }

    #region Selection and Caps Interaction

    [Fact]
    public void SelectionAndCaps_TargetExceedsCap_CapsWin()
    {
        // Arrange: density target would select 6, but cap is 4
        var preset = CreatePresetWithCaps(maxHitsPerBar: 4);

        var candidates = new List<GrooveOnset>
        {
            CreateOnset(1.0m, 100),
            CreateOnset(1.5m, 95),
            CreateOnset(2.0m, 90),
            CreateOnset(2.5m, 85),
            CreateOnset(3.0m, 80),
            CreateOnset(3.5m, 75)
        };

        var barPlan = new GrooveBarPlan
        {
            BarNumber = 1,
            BaseOnsets = new List<GrooveOnset>(),
            SelectedVariationOnsets = candidates,
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
            mergePolicy: new GrooveOverrideMergePolicy());

        // Assert: caps enforced, only 4 remain
        Assert.Equal(4, result.FinalOnsets.Count);
        // Highest velocities kept
        Assert.Contains(result.FinalOnsets, o => o.Velocity == 100);
        Assert.Contains(result.FinalOnsets, o => o.Velocity == 95);
    }

    #endregion

    #region Velocity and Timing Pipeline Order

    [Fact]
    public void VelocityAndTiming_BothApplied_IndependentlyCorrect()
    {
        // Arrange: verify velocity and timing are computed independently
        var policy = CreateTimingPolicy();
        var accentPolicy = CreateAccentPolicy();

        var onset = new GrooveOnset
        {
            Role = "Kick",
            BarNumber = 1,
            Beat = 1.5m, // Offbeat
            Strength = null, // Will be classified
            Velocity = null,
            TimingOffsetTicks = null
        };

        // Act: compute strength and velocity
        var strength = OnsetStrengthClassifier.Classify(
            onset.Beat,
            beatsPerBar: 4,
            AllowedSubdivision.Quarter | AllowedSubdivision.Eighth);

        int velocity = VelocityShaper.ComputeVelocity("Kick", strength, accentPolicy, null);

        // Apply role timing via the engine
        var onsets = new List<GrooveOnset> { onset with { Strength = strength, Velocity = velocity } };
        var afterTiming = RoleTimingEngine.ApplyRoleTiming(onsets, policy, null);
        int timing = afterTiming[0].TimingOffsetTicks ?? 0;

        // Assert: both computed correctly
        Assert.Equal(OnsetStrength.Offbeat, strength);
        Assert.Equal(85, velocity); // From accent policy
        Assert.Equal(0, timing); // OnTop feel + 0 bias
    }

    [Fact]
    public void VelocityAndTiming_OrderDoesNotAffectResult()
    {
        // Arrange
        var policy = CreateTimingPolicy();
        var accentPolicy = CreateAccentPolicy();
        decimal beat = 2.0m;

        // Act: compute timing first, then velocity
        var strength1 = OnsetStrengthClassifier.Classify(beat, 4, AllowedSubdivision.Quarter | AllowedSubdivision.Eighth);
        var onsets1 = new List<GrooveOnset> { CreateOnset(beat, 0) with { Strength = strength1 } };
        var afterTiming1 = RoleTimingEngine.ApplyRoleTiming(onsets1, policy, null);
        int timing1 = afterTiming1[0].TimingOffsetTicks ?? 0;
        int velocity1 = VelocityShaper.ComputeVelocity("Kick", strength1, accentPolicy, null);

        // Compute velocity first, then timing
        var strength2 = OnsetStrengthClassifier.Classify(beat, 4, AllowedSubdivision.Quarter | AllowedSubdivision.Eighth);
        int velocity2 = VelocityShaper.ComputeVelocity("Kick", strength2, accentPolicy, null);
        var onsets2 = new List<GrooveOnset> { CreateOnset(beat, velocity2) with { Strength = strength2 } };
        var afterTiming2 = RoleTimingEngine.ApplyRoleTiming(onsets2, policy, null);
        int timing2 = afterTiming2[0].TimingOffsetTicks ?? 0;

        // Assert: same results regardless of order
        Assert.Equal(strength1, strength2);
        Assert.Equal(velocity1, velocity2);
        Assert.Equal(timing1, timing2);
    }

    #endregion

    #region Snapshot Helper Verification

    [Fact]
    public void SnapshotHelper_RoundTrip_PreservesData()
    {
        // Arrange
        var plan = new GrooveBarPlan
        {
            BarNumber = 5,
            BaseOnsets = new List<GrooveOnset>(),
            SelectedVariationOnsets = new List<GrooveOnset>(),
            FinalOnsets = new List<GrooveOnset>
            {
                new GrooveOnset
                {
                    Role = "Kick",
                    BarNumber = 5,
                    Beat = 1.0m,
                    Strength = OnsetStrength.Downbeat,
                    Velocity = 110,
                    TimingOffsetTicks = 0
                },
                new GrooveOnset
                {
                    Role = "Kick",
                    BarNumber = 5,
                    Beat = 2.5m,
                    Strength = OnsetStrength.Offbeat,
                    Velocity = 85,
                    TimingOffsetTicks = 10
                }
            }
        };

        // Act: serialize and deserialize
        var snapshot = GrooveSnapshotHelper.CreateSnapshot(plan, "Kick");
        string json = GrooveSnapshotHelper.SerializeSnapshot(snapshot);
        var restored = GrooveSnapshotHelper.DeserializeSnapshot(json);

        // Assert: data preserved
        Assert.NotNull(restored);
        Assert.True(GrooveSnapshotHelper.SnapshotsEqual(snapshot, restored!));
    }

    [Fact]
    public void SnapshotHelper_DetectsDifferences_Correctly()
    {
        // Arrange
        var snapshot1 = new GrooveSnapshotHelper.BarPlanSnapshot
        {
            BarNumber = 1,
            Role = "Kick",
            Onsets = new List<GrooveSnapshotHelper.OnsetSnapshot>
            {
                new() { Beat = 1.0m, Velocity = 100, TimingOffsetTicks = 0 }
            }
        };

        var snapshot2 = new GrooveSnapshotHelper.BarPlanSnapshot
        {
            BarNumber = 1,
            Role = "Kick",
            Onsets = new List<GrooveSnapshotHelper.OnsetSnapshot>
            {
                new() { Beat = 1.0m, Velocity = 110, TimingOffsetTicks = 0 } // Different velocity
            }
        };

        // Act
        var differences = GrooveSnapshotHelper.GetSnapshotDifferences(snapshot1, snapshot2);

        // Assert: difference detected
        Assert.Single(differences);
        Assert.Contains("Velocity", differences[0]);
    }

    #endregion

    #region Feel Timing Edge Cases

    [Fact]
    public void FeelTiming_StraightFeel_NoShiftApplied()
    {
        // Arrange: straight feel should never shift
        var policy = new GrooveSubdivisionPolicy
        {
            Feel = GrooveFeel.Straight,
            SwingAmount01 = 0.5, // Ignored for Straight
            AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth
        };

        var onsets = new List<GrooveOnset> { CreateOnset(1.5m, 80) }; // Eighth offbeat

        // Act
        var result = FeelTimingEngine.ApplyFeelTiming(onsets, policy, null, null);

        // Assert: no shift (returns same timing offset)
        Assert.Equal(onsets[0].TimingOffsetTicks ?? 0, result[0].TimingOffsetTicks ?? 0);
    }

    [Fact]
    public void FeelTiming_SwingFeel_OnlyAffectsOffbeats()
    {
        // Arrange
        var policy = new GrooveSubdivisionPolicy
        {
            Feel = GrooveFeel.Swing,
            SwingAmount01 = 0.5,
            AllowedSubdivisions = AllowedSubdivision.Quarter | AllowedSubdivision.Eighth
        };

        var downbeat = new List<GrooveOnset> { CreateOnset(1.0m, 100) };
        var offbeat = new List<GrooveOnset> { CreateOnset(1.5m, 85) };

        // Act
        var downbeatResult = FeelTimingEngine.ApplyFeelTiming(downbeat, policy, null, null);
        var offbeatResult = FeelTimingEngine.ApplyFeelTiming(offbeat, policy, null, null);

        int downbeatOffset = downbeatResult[0].TimingOffsetTicks ?? 0;
        int offbeatOffset = offbeatResult[0].TimingOffsetTicks ?? 0;

        // Assert: downbeat unaffected, offbeat shifted
        Assert.Equal(0, downbeatOffset);
        Assert.True(offbeatOffset > 0, "Swing should shift offbeats later");
    }

    #endregion

    #region Test Helpers

    private static GroovePresetDefinition CreatePresetWithCaps(int maxHitsPerBar)
    {
        return new GroovePresetDefinition
        {
            Identity = new GroovePresetIdentity { Name = "Test", BeatsPerBar = 4 },
            AnchorLayer = new GrooveInstanceLayer(),
            ProtectionPolicy = new GrooveProtectionPolicy
            {
                RoleConstraintPolicy = new GrooveRoleConstraintPolicy
                {
                    RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
                    {
                        ["Kick"] = new RoleRhythmVocabulary
                        {
                            MaxHitsPerBar = maxHitsPerBar,
                            MaxHitsPerBeat = 4
                        }
                    }
                }
            },
            VariationCatalog = new GrooveVariationCatalog
            {
                Identity = new GroovePresetIdentity { Name = "TestCatalog" },
                HierarchyLayers = new List<GrooveVariationLayer>()
            }
        };
    }

    private static GrooveTimingPolicy CreateTimingPolicy()
    {
        return new GrooveTimingPolicy
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
        };
    }

    private static GrooveAccentPolicy CreateAccentPolicy()
    {
        return new GrooveAccentPolicy
        {
            RoleStrengthVelocity = new Dictionary<string, Dictionary<OnsetStrength, VelocityRule>>
            {
                ["Kick"] = new Dictionary<OnsetStrength, VelocityRule>
                {
                    [OnsetStrength.Downbeat] = new VelocityRule { Typical = 100, AccentBias = 10, Min = 80, Max = 127 },
                    [OnsetStrength.Offbeat] = new VelocityRule { Typical = 85, AccentBias = 0, Min = 60, Max = 110 },
                    [OnsetStrength.Strong] = new VelocityRule { Typical = 95, AccentBias = 5, Min = 70, Max = 120 }
                }
            }
        };
    }

    private static GrooveOnset CreateOnset(decimal beat, int velocity)
    {
        return new GrooveOnset
        {
            Role = "Kick",
            BarNumber = 1,
            Beat = beat,
            Strength = OnsetStrength.Offbeat,
            Velocity = velocity,
            TimingOffsetTicks = 0
        };
    }

    #endregion
}

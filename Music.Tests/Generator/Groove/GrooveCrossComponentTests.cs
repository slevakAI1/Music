// AI: purpose=Story H1 cross-component tests verifying pipeline ordering and interactions.
// AI: invariants=Deterministic; tests component boundaries; fast.
// AI: deps=FeelTimingEngine, RoleTimingEngine, OnsetStrengthClassifier, DrumCapsEnforcer (moved Story 4.3).

using Music.Generator;
using Music.Generator.Agents.Drums;
using Music.Generator.Groove;
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

        var enforcer = new DrumCapsEnforcer();

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


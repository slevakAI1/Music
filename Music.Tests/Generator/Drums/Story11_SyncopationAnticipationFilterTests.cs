// AI: purpose=Unit tests for Story 11 syncopation/anticipation filter; validates rhythm vocabulary rules enforcement.
// AI: deps=DrumTrackGeneratorNew.ApplySyncopationAnticipationFilter; uses reflection to test private methods.
// AI: coverage=AllowSyncopation true/false, AllowAnticipation true/false, offbeat/pickup detection, role-specific filtering.

using FluentAssertions;
using Music.Generator;
using System.Reflection;

namespace Music.Tests.Generator.Drums;

public class Story11_SyncopationAnticipationFilterTests
{
    private readonly MethodInfo _filterMethod;
    private readonly MethodInfo _isOffbeatMethod;
    private readonly MethodInfo _isPickupMethod;

    public Story11_SyncopationAnticipationFilterTests()
    {
        var type = typeof(DrumTrackGenerator);
        
        _filterMethod = type.GetMethod(
            "ApplySyncopationAnticipationFilter",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ApplySyncopationAnticipationFilter method not found");
        
        _isOffbeatMethod = type.GetMethod(
            "IsOffbeatPosition",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("IsOffbeatPosition method not found");
        
        _isPickupMethod = type.GetMethod(
            "IsPickupPosition",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("IsPickupPosition method not found");
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_NullOnsets_ReturnsEmpty()
    {
        var result = InvokeFilter(null, CreateDefaultPolicy(), 4);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_EmptyOnsets_ReturnsEmpty()
    {
        var result = InvokeFilter(new List<DrumOnset>(), CreateDefaultPolicy(), 4);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_NullPolicy_ReturnsAllOnsets()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Kick, 1, 1.0m, 100, 0),
            new DrumOnset(DrumRole.Snare, 1, 2.5m, 100, 0)
        };

        var result = InvokeFilter(onsets, null, 4);
        result.Should().HaveCount(2);
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_NoVocabularyForRole_AllowsOnsets()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Kick, 1, 1.5m, 100, 0)  // Offbeat
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>()
            // No entry for Kick
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(1);
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_AllowSyncopationTrue_KeepsOffbeats()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.ClosedHat, 1, 1.0m, 100, 0),
            new DrumOnset(DrumRole.ClosedHat, 1, 1.5m, 100, 0),  // Offbeat
            new DrumOnset(DrumRole.ClosedHat, 1, 2.0m, 100, 0),
            new DrumOnset(DrumRole.ClosedHat, 1, 2.5m, 100, 0)   // Offbeat
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["ClosedHat"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = true,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(4, "All onsets should be kept when syncopation allowed");
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_AllowSyncopationFalse_FiltersOffbeats()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.ClosedHat, 1, 1.0m, 100, 0),
            new DrumOnset(DrumRole.ClosedHat, 1, 1.5m, 100, 0),  // Offbeat - should be filtered
            new DrumOnset(DrumRole.ClosedHat, 1, 2.0m, 100, 0),
            new DrumOnset(DrumRole.ClosedHat, 1, 2.5m, 100, 0)   // Offbeat - should be filtered
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["ClosedHat"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = false,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(2, "Offbeats should be filtered");
        result.Should().OnlyContain(o => o.Beat == 1.0m || o.Beat == 2.0m, "Only downbeats should remain");
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_AllowAnticipationTrue_KeepsPickups()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Snare, 1, 4.0m, 100, 0),
            new DrumOnset(DrumRole.Snare, 1, 4.75m, 100, 0)  // Pickup
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["Snare"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = true,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(2, "All onsets should be kept when anticipation allowed");
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_AllowAnticipationFalse_FiltersPickups()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Kick, 1, 1.0m, 100, 0),
            new DrumOnset(DrumRole.Kick, 1, 4.75m, 100, 0),  // Pickup - should be filtered
            new DrumOnset(DrumRole.Kick, 1, 2.75m, 100, 0)   // Pickup - should be filtered
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["Kick"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = true,
                    AllowAnticipation = false,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(1, "Pickups should be filtered");
        result[0].Beat.Should().Be(1.0m);
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_BothFalse_FiltersBoth()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Snare, 1, 2.0m, 100, 0),      // Strong beat - keep
            new DrumOnset(DrumRole.Snare, 1, 2.5m, 100, 0),      // Offbeat - filter
            new DrumOnset(DrumRole.Snare, 1, 4.75m, 100, 0)      // Pickup - filter
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["Snare"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = false,
                    AllowAnticipation = false,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(1);
        result[0].Beat.Should().Be(2.0m);
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_DifferentRolesHaveDifferentRules()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Kick, 1, 1.5m, 100, 0),        // Kick offbeat
            new DrumOnset(DrumRole.ClosedHat, 1, 1.5m, 100, 0)    // Hat offbeat
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["Kick"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = false,  // Kick: no offbeats
                    AllowAnticipation = true,
                    MaxHitsPerBar = 8,
                    MaxHitsPerBeat = 2,
                    SnapStrongBeatsToChordTones = false
                },
                ["ClosedHat"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = true,   // Hat: offbeats OK
                    AllowAnticipation = true,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(1, "Only hat offbeat should remain");
        result[0].Role.Should().Be(DrumRole.ClosedHat);
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_PreservesOnsetProperties()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Snare, 5, 2.0m, 95, 1920)
            {
                IsMustHit = true,
                IsProtected = true
            }
        };

        var policy = CreateDefaultPolicy();
        var result = InvokeFilter(onsets, policy, 4);

        result.Should().HaveCount(1);
        var onset = result[0];
        onset.Role.Should().Be(DrumRole.Snare);
        onset.BarNumber.Should().Be(5);
        onset.Beat.Should().Be(2.0m);
        onset.Velocity.Should().Be(95);
        onset.TickPosition.Should().Be(1920);
        onset.IsMustHit.Should().BeTrue();
        onset.IsProtected.Should().BeTrue();
    }

    [Fact]
    public void IsOffbeatPosition_DetectsEighthNoteOffbeats()
    {
        InvokeIsOffbeat(1.5m, 4).Should().BeTrue("1.5 should be offbeat");
        InvokeIsOffbeat(2.5m, 4).Should().BeTrue("2.5 should be offbeat");
        InvokeIsOffbeat(3.5m, 4).Should().BeTrue("3.5 should be offbeat");
        InvokeIsOffbeat(4.5m, 4).Should().BeTrue("4.5 should be offbeat");
    }

    [Fact]
    public void IsOffbeatPosition_DoesNotDetectDownbeats()
    {
        InvokeIsOffbeat(1.0m, 4).Should().BeFalse("1.0 should not be offbeat");
        InvokeIsOffbeat(2.0m, 4).Should().BeFalse("2.0 should not be offbeat");
        InvokeIsOffbeat(3.0m, 4).Should().BeFalse("3.0 should not be offbeat");
        InvokeIsOffbeat(4.0m, 4).Should().BeFalse("4.0 should not be offbeat");
    }

    [Fact]
    public void IsOffbeatPosition_DoesNotDetectSixteenths()
    {
        InvokeIsOffbeat(1.25m, 4).Should().BeFalse("1.25 should not be offbeat");
        InvokeIsOffbeat(1.75m, 4).Should().BeFalse("1.75 should not be offbeat");
    }

    [Fact]
    public void IsPickupPosition_Detects75Positions()
    {
        InvokeIsPickup(1.75m, 4).Should().BeTrue("1.75 should be pickup");
        InvokeIsPickup(2.75m, 4).Should().BeTrue("2.75 should be pickup");
        InvokeIsPickup(4.75m, 4).Should().BeTrue("4.75 should be pickup");
    }

    [Fact]
    public void IsPickupPosition_DoesNotDetectDownbeats()
    {
        InvokeIsPickup(1.0m, 4).Should().BeFalse();
        InvokeIsPickup(2.0m, 4).Should().BeFalse();
    }

    [Fact]
    public void IsPickupPosition_DoesNotDetectOffbeats()
    {
        InvokeIsPickup(1.5m, 4).Should().BeFalse();
        InvokeIsPickup(2.5m, 4).Should().BeFalse();
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_ThreeQuarterTime_Works()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.ClosedHat, 1, 1.0m, 100, 0),
            new DrumOnset(DrumRole.ClosedHat, 1, 1.5m, 100, 0),  // Offbeat
            new DrumOnset(DrumRole.ClosedHat, 1, 2.0m, 100, 0),
            new DrumOnset(DrumRole.ClosedHat, 1, 2.5m, 100, 0),  // Offbeat
            new DrumOnset(DrumRole.ClosedHat, 1, 3.0m, 100, 0)
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["ClosedHat"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = false,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 3);  // 3/4 time
        result.Should().HaveCount(3, "Only downbeats should remain in 3/4");
        result.Should().OnlyContain(o => o.Beat == 1.0m || o.Beat == 2.0m || o.Beat == 3.0m);
    }

    // Helper methods
    private List<DrumOnset> InvokeFilter(
        List<DrumOnset>? onsets, 
        GrooveRoleConstraintPolicy? policy, 
        int beatsPerBar)
    {
        var result = _filterMethod.Invoke(null, new object?[] { onsets, policy, beatsPerBar });
        return (List<DrumOnset>)(result ?? new List<DrumOnset>());
    }

    private bool InvokeIsOffbeat(decimal beat, int beatsPerBar)
    {
        var result = _isOffbeatMethod.Invoke(null, new object[] { beat, beatsPerBar });
        return (bool)(result ?? false);
    }

    private bool InvokeIsPickup(decimal beat, int beatsPerBar)
    {
        var result = _isPickupMethod.Invoke(null, new object[] { beat, beatsPerBar });
        return (bool)(result ?? false);
    }

    private static GrooveRoleConstraintPolicy CreateDefaultPolicy()
    {
        return new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["Kick"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = true,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 8,
                    MaxHitsPerBeat = 2,
                    SnapStrongBeatsToChordTones = false
                },
                ["Snare"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = true,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 8,
                    MaxHitsPerBeat = 2,
                    SnapStrongBeatsToChordTones = false
                },
                ["ClosedHat"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = true,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 32,
                    MaxHitsPerBeat = 4,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };
    }
}


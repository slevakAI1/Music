// AI: purpose=Unit tests for Story 11 syncopation/anticipation filter; validates rhythm vocabulary rules enforcement.
// AI: Story G3: Updated to use shared RhythmVocabularyFilter instead of private DrumTrackGenerator methods.
// AI: coverage=AllowSyncopation true/false, AllowAnticipation true/false, offbeat/pickup detection, role-specific filtering.

using FluentAssertions;
using Music.Generator;
using Music.Generator.Groove;

namespace Music.Tests.Generator.Drums;

public class Story11_SyncopationAnticipationFilterTests
{
    #region Helper Methods

    private static List<DrumOnset> InvokeFilter(List<DrumOnset>? onsets, GrooveRoleConstraintPolicy? policy, int beatsPerBar)
    {
        return RhythmVocabularyFilter.Filter(
            onsets ?? new List<DrumOnset>(),
            onset => onset.Role.ToString(),
            onset => onset.Beat,
            beatsPerBar,
            policy);
    }

    private static GrooveRoleConstraintPolicy CreateDefaultPolicy()
    {
        return new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>()
        };
    }

    #endregion

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
            new DrumOnset(DrumRole.Kick, 5, 3.25m, 110, 1920) { IsMustHit = true }
        };

        var policy = CreateDefaultPolicy();

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(1);
        result[0].BarNumber.Should().Be(5);
        result[0].Velocity.Should().Be(110);
        result[0].TickPosition.Should().Be(1920);
        result[0].IsMustHit.Should().BeTrue();
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_MultipleBars()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Kick, 1, 1.0m, 100, 0),
            new DrumOnset(DrumRole.Kick, 1, 1.5m, 100, 0),  // Offbeat
            new DrumOnset(DrumRole.Kick, 2, 1.0m, 100, 0),
            new DrumOnset(DrumRole.Kick, 2, 1.5m, 100, 0)   // Offbeat
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["Kick"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = false,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 8,
                    MaxHitsPerBeat = 2,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 4);
        result.Should().HaveCount(2, "Offbeats should be filtered from both bars");
        result.Should().OnlyContain(o => o.Beat == 1.0m);
    }

    [Fact]
    public void ApplySyncopationAnticipationFilter_3_4Time()
    {
        var onsets = new List<DrumOnset>
        {
            new DrumOnset(DrumRole.Kick, 1, 1.0m, 100, 0),
            new DrumOnset(DrumRole.Kick, 1, 1.5m, 100, 0),  // Offbeat
            new DrumOnset(DrumRole.Kick, 1, 2.0m, 100, 0),
            new DrumOnset(DrumRole.Kick, 1, 2.5m, 100, 0)   // Offbeat
        };

        var policy = new GrooveRoleConstraintPolicy
        {
            RoleVocabulary = new Dictionary<string, RoleRhythmVocabulary>
            {
                ["Kick"] = new RoleRhythmVocabulary
                {
                    AllowSyncopation = false,
                    AllowAnticipation = true,
                    MaxHitsPerBar = 8,
                    MaxHitsPerBeat = 2,
                    SnapStrongBeatsToChordTones = false
                }
            }
        };

        var result = InvokeFilter(onsets, policy, 3);
        result.Should().HaveCount(2);
        result.Should().OnlyContain(o => o.Beat == 1.0m || o.Beat == 2.0m);
    }

    #region Position Detection Tests (now public via RhythmVocabularyFilter)

    [Fact]
    public void IsOffbeatPosition_DetectsOffbeats()
    {
        RhythmVocabularyFilter.IsOffbeatPosition(1.5m, 4).Should().BeTrue();
        RhythmVocabularyFilter.IsOffbeatPosition(2.5m, 4).Should().BeTrue();
        RhythmVocabularyFilter.IsOffbeatPosition(3.5m, 4).Should().BeTrue();
        RhythmVocabularyFilter.IsOffbeatPosition(4.5m, 4).Should().BeTrue();
    }

    [Fact]
    public void IsOffbeatPosition_DoesNotDetectDownbeats()
    {
        RhythmVocabularyFilter.IsOffbeatPosition(1.0m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsOffbeatPosition(2.0m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsOffbeatPosition(3.0m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsOffbeatPosition(4.0m, 4).Should().BeFalse();
    }

    [Fact]
    public void IsOffbeatPosition_DoesNotDetectSixteenths()
    {
        RhythmVocabularyFilter.IsOffbeatPosition(1.25m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsOffbeatPosition(1.75m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsOffbeatPosition(2.25m, 4).Should().BeFalse();
    }

    [Fact]
    public void IsPickupPosition_DetectsPickups()
    {
        RhythmVocabularyFilter.IsPickupPosition(1.75m, 4).Should().BeTrue();
        RhythmVocabularyFilter.IsPickupPosition(2.75m, 4).Should().BeTrue();
        RhythmVocabularyFilter.IsPickupPosition(3.75m, 4).Should().BeTrue();
        RhythmVocabularyFilter.IsPickupPosition(4.75m, 4).Should().BeTrue();
    }

    [Fact]
    public void IsPickupPosition_DoesNotDetectOffbeats()
    {
        RhythmVocabularyFilter.IsPickupPosition(1.5m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsPickupPosition(2.5m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsPickupPosition(3.5m, 4).Should().BeFalse();
        RhythmVocabularyFilter.IsPickupPosition(4.5m, 4).Should().BeFalse();
    }

    #endregion
}

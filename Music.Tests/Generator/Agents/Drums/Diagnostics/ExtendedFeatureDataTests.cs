// AI: purpose=Unit tests for Story 7.2b extended feature data container and serialization.
// AI: deps=Tests DrumTrackExtendedFeatureDataBuilder, DrumExtendedFeatureDataSerializer; uses xUnit, FluentAssertions.
// AI: change=Story 7.2b; extend tests when adding new analysis components.

using FluentAssertions;
using Music.Generator.Agents.Drums.Diagnostics;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Unit tests for extended feature data, velocity dynamics, timing feel, and serialization (Story 7.2b).
/// </summary>
public class ExtendedFeatureDataTests
{
    #region DrumTrackExtendedFeatureDataBuilder

    [Fact]
    public void Build_WithValidBaseData_ProducesCompleteExtendedData()
    {
        // Arrange
        var baseData = CreateTestBaseData();

        // Act
        var result = DrumTrackExtendedFeatureDataBuilder.Build(baseData, includePopRockReference: true);

        // Assert
        result.BaseData.Should().BeSameAs(baseData);
        result.TrackId.Should().Be("test-track");
        result.TotalBars.Should().Be(4);

        // All analysis sections should be populated
        result.PatternRepetition.Should().NotBeNull();
        result.PatternSimilarity.Should().NotBeNull();
        result.SequencePatterns.Should().NotBeNull();
        result.CrossRoleCoordination.Should().NotBeNull();
        result.AnchorCandidates.Should().NotBeNull();
        result.StructuralMarkers.Should().NotBeNull();
        result.VelocityDynamics.Should().NotBeNull();
        result.TimingFeel.Should().NotBeNull();

        result.SchemaVersion.Should().Be("1.0");
    }

    [Fact]
    public void Build_WithOptions_RespectsAnalysisFlags()
    {
        // Arrange
        var baseData = CreateTestBaseData();
        var options = new ExtendedAnalysisOptions
        {
            IncludeCrossRoleAnalysis = false,
            IncludeAnchorAnalysis = false,
            IncludeStructuralAnalysis = false,
            IncludePerformanceAnalysis = false
        };

        // Act
        var result = DrumTrackExtendedFeatureDataBuilder.Build(baseData, options);

        // Assert
        // Pattern analysis always runs
        result.PatternRepetition.Should().NotBeNull();

        // Disabled analyses should have empty results
        result.CrossRoleCoordination.CoincidenceCount.Should().BeEmpty();
        result.AnchorCandidates.RoleAnchors.Should().BeEmpty();
        result.StructuralMarkers.TotalMarkers.Should().Be(0);
        result.VelocityDynamics.RoleDistributions.Should().BeEmpty();
    }

    #endregion

    #region VelocityDynamicsExtractor

    [Fact]
    public void Extract_WithVariedVelocities_ComputesDistributionsAndAccents()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            // Kick with varied velocities
            CreateEvent(1, 1.0m, "Kick", 110),  // Accent
            CreateEvent(1, 3.0m, "Kick", 100),
            CreateEvent(2, 1.0m, "Kick", 105),
            CreateEvent(2, 3.0m, "Kick", 95),

            // Snare with ghost notes
            CreateEvent(1, 2.0m, "Snare", 100),
            CreateEvent(1, 2.75m, "Snare", 40),  // Ghost
            CreateEvent(1, 4.0m, "Snare", 105),
            CreateEvent(2, 2.0m, "Snare", 100),
            CreateEvent(2, 3.75m, "Snare", 35),  // Ghost
            CreateEvent(2, 4.0m, "Snare", 110)
        };

        var baseData = CreateBaseDataWithEvents(events);

        // Act
        var result = VelocityDynamicsExtractor.Extract(baseData);

        // Assert
        result.RoleDistributions.Should().ContainKey("Kick");
        result.RoleDistributions.Should().ContainKey("Snare");

        result.RoleDistributions["Kick"].Mean.Should().BeGreaterThan(90);
        result.RoleDistributions["Snare"].StdDev.Should().BeGreaterThan(0); // Has variation

        // Ghost positions should be detected for snare
        result.GhostPositions.Should().NotBeEmpty();
    }

    #endregion

    #region TimingFeelExtractor

    [Fact]
    public void Extract_WithTimingOffsets_ComputesFeelMetrics()
    {
        // Arrange - Events with laid-back timing
        var events = new List<DrumMidiEvent>
        {
            CreateEventWithTiming(1, 1.0m, "Kick", 100, 5),   // Behind
            CreateEventWithTiming(1, 2.0m, "Snare", 100, 8),  // More behind
            CreateEventWithTiming(1, 3.0m, "Kick", 100, 4),
            CreateEventWithTiming(1, 4.0m, "Snare", 100, 7),
            CreateEventWithTiming(2, 1.0m, "Kick", 100, 6),
            CreateEventWithTiming(2, 2.0m, "Snare", 100, 9),
            CreateEventWithTiming(2, 3.0m, "Kick", 100, 5),
            CreateEventWithTiming(2, 4.0m, "Snare", 100, 8)
        };

        var baseData = CreateBaseDataWithEvents(events);

        // Act
        var result = TimingFeelExtractor.Extract(baseData);

        // Assert
        result.RoleAverageOffset.Should().ContainKey("Kick");
        result.RoleAverageOffset.Should().ContainKey("Snare");

        // Should detect laid-back feel (positive offset)
        result.AheadBehindScore.Should().BeGreaterThan(0);
        result.IsLaidBack.Should().BeTrue();
        result.IsPushed.Should().BeFalse();

        result.TimingConsistency.Should().BeGreaterThan(0);
    }

    #endregion

    #region Serialization Round-Trip

    [Fact]
    public void SerializeDeserialize_PreservesAllData()
    {
        // Arrange
        var baseData = CreateTestBaseData();
        var extendedData = DrumTrackExtendedFeatureDataBuilder.Build(baseData);

        // Act
        var json = DrumExtendedFeatureDataSerializer.Serialize(extendedData);
        var restored = DrumExtendedFeatureDataSerializer.Deserialize(json);

        // Assert
        restored.TrackId.Should().Be(extendedData.TrackId);
        restored.TotalBars.Should().Be(extendedData.TotalBars);
        restored.BaseData.Events.Count.Should().Be(extendedData.BaseData.Events.Count);

        restored.PatternRepetition.UniquePatternCount.Should().Be(extendedData.PatternRepetition.UniquePatternCount);
        restored.PatternRepetition.TotalBars.Should().Be(extendedData.PatternRepetition.TotalBars);

        restored.TimingFeel.SwingRatio.Should().Be(extendedData.TimingFeel.SwingRatio);
    }

    [Fact]
    public void TryDeserialize_WithInvalidJson_ReturnsFalse()
    {
        var success = DrumExtendedFeatureDataSerializer.TryDeserialize(
            "{ invalid json }",
            out var data,
            out var error);

        success.Should().BeFalse();
        data.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private static DrumTrackFeatureData CreateTestBaseData()
    {
        var events = new List<DrumMidiEvent>
        {
            CreateEvent(1, 1.0m, "Kick", 100),
            CreateEvent(1, 2.0m, "Snare", 100),
            CreateEvent(1, 3.0m, "Kick", 100),
            CreateEvent(1, 4.0m, "Snare", 100),
            CreateEvent(2, 1.0m, "Kick", 100),
            CreateEvent(2, 2.0m, "Snare", 100),
            CreateEvent(2, 3.0m, "Kick", 100),
            CreateEvent(2, 4.0m, "Snare", 100),
            CreateEvent(3, 1.0m, "Kick", 100),
            CreateEvent(3, 2.0m, "Snare", 100),
            CreateEvent(3, 3.0m, "Kick", 100),
            CreateEvent(3, 4.0m, "Snare", 100),
            CreateEvent(4, 1.0m, "Kick", 100),
            CreateEvent(4, 2.0m, "Snare", 100),
            CreateEvent(4, 3.0m, "Kick", 100),
            CreateEvent(4, 4.0m, "Snare", 100)
        };

        return CreateBaseDataWithEvents(events);
    }

    private static DrumTrackFeatureData CreateBaseDataWithEvents(List<DrumMidiEvent> events)
    {
        var totalBars = events.Max(e => e.BarNumber);
        var barPatterns = Enumerable.Range(1, totalBars)
            .Select(bar => BarPatternExtractor.Extract(events.Where(e => e.BarNumber == bar).ToList(), bar, 4))
            .ToList();

        var barStats = Enumerable.Range(1, totalBars).Select(bar =>
        {
            var barEvents = events.Where(e => e.BarNumber == bar).ToList();
            return new BarOnsetStats
            {
                BarNumber = bar,
                TotalHits = barEvents.Count,
                HitsPerRole = barEvents.GroupBy(e => e.Role).ToDictionary(g => g.Key, g => g.Count()),
                AverageVelocity = barEvents.Any() ? barEvents.Average(e => e.Velocity) : 0,
                MinVelocity = barEvents.Any() ? barEvents.Min(e => e.Velocity) : 0,
                MaxVelocity = barEvents.Any() ? barEvents.Max(e => e.Velocity) : 0,
                AverageVelocityPerRole = barEvents.GroupBy(e => e.Role)
                    .ToDictionary(g => g.Key, g => g.Average(e => (double)e.Velocity)),
                AverageTimingOffset = 0,
                MinTimingOffset = 0,
                MaxTimingOffset = 0,
                HitsPerBeat = new[] { 0, 0, 0, 0 },
                OffbeatRatio = 0
            };
        }).ToList();

        var roles = events.Select(e => e.Role).Distinct().ToHashSet();
        var roleMatrices = new Dictionary<string, BeatPositionMatrix>();

        foreach (var role in roles)
        {
            var barSlots = new List<BeatPositionSlot?[]>();
            for (int bar = 1; bar <= totalBars; bar++)
            {
                var slots = new BeatPositionSlot?[16];
                foreach (var evt in events.Where(e => e.BarNumber == bar && e.Role == role))
                {
                    var pos = BarPatternExtractor.CalculateGridPosition(evt.Beat, 4, 16);
                    if (pos < 16)
                        slots[pos] = new BeatPositionSlot(evt.Velocity, evt.TimingOffsetTicks ?? 0);
                }
                barSlots.Add(slots);
            }

            roleMatrices[role] = new BeatPositionMatrix
            {
                Role = role,
                TotalBars = totalBars,
                GridResolution = 16,
                BarSlots = barSlots
            };
        }

        return new DrumTrackFeatureData
        {
            TrackId = "test-track",
            TotalBars = totalBars,
            DefaultBeatsPerBar = 4,
            TempoEstimateBpm = 120,
            Events = events,
            BarPatterns = barPatterns,
            BarStats = barStats,
            RoleMatrices = roleMatrices,
            ActiveRoles = roles
        };
    }

    private static DrumMidiEvent CreateEvent(int bar, decimal beat, string role, int velocity)
    {
        return new DrumMidiEvent
        {
            BarNumber = bar,
            Beat = beat,
            Role = role,
            MidiNote = role == "Kick" ? 36 : 38,
            Velocity = velocity,
            DurationTicks = 120,
            AbsoluteTimeTicks = (bar - 1) * 1920 + (int)((beat - 1) * 480),
            TimingOffsetTicks = null
        };
    }

    private static DrumMidiEvent CreateEventWithTiming(int bar, decimal beat, string role, int velocity, int timingOffset)
    {
        return new DrumMidiEvent
        {
            BarNumber = bar,
            Beat = beat,
            Role = role,
            MidiNote = role == "Kick" ? 36 : 38,
            Velocity = velocity,
            DurationTicks = 120,
            AbsoluteTimeTicks = (bar - 1) * 1920 + (int)((beat - 1) * 480) + timingOffset,
            TimingOffsetTicks = timingOffset
        };
    }

    #endregion
}


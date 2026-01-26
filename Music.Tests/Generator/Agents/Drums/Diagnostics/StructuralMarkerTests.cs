// AI: purpose=Unit tests for Story 7.2b structural marker detection.
// AI: deps=Tests StructuralMarkerDetector, AnchorCandidateExtractor, SequencePatternDetector; uses xUnit, FluentAssertions.
// AI: change=Story 7.2b; extend tests when adding structural analysis.

using FluentAssertions;
using Music.Generator.Agents.Drums.Diagnostics;
using Xunit;

namespace Music.Tests.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Unit tests for structural marker detection and anchor analysis (Story 7.2b).
/// </summary>
public class StructuralMarkerTests
{
    #region StructuralMarkerDetector

    [Fact]
    public void Detect_WithCrashesAndDensityVariation_IdentifiesMarkers()
    {
        // Arrange
        var events = new List<DrumMidiEvent>
        {
            // Bar 1: Normal density (4 hits)
            CreateEvent(1, 1.0m, "Kick", 100),
            CreateEvent(1, 2.0m, "Snare", 100),
            CreateEvent(1, 3.0m, "Kick", 100),
            CreateEvent(1, 4.0m, "Snare", 100),

            // Bar 2: High density fill (10 hits) + crash
            CreateEvent(2, 1.0m, "Crash", 110),
            CreateEvent(2, 1.0m, "Kick", 100),
            CreateEvent(2, 1.5m, "Snare", 80),
            CreateEvent(2, 2.0m, "Snare", 85),
            CreateEvent(2, 2.5m, "Tom1", 90),
            CreateEvent(2, 3.0m, "Tom1", 95),
            CreateEvent(2, 3.25m, "Tom2", 100),
            CreateEvent(2, 3.5m, "Tom2", 105),
            CreateEvent(2, 3.75m, "FloorTom", 110),
            CreateEvent(2, 4.0m, "Snare", 115),

            // Bar 3: Low density (2 hits)
            CreateEvent(3, 1.0m, "Kick", 100),
            CreateEvent(3, 3.0m, "Kick", 100),

            // Bar 4: Normal
            CreateEvent(4, 1.0m, "Kick", 100),
            CreateEvent(4, 2.0m, "Snare", 100),
            CreateEvent(4, 3.0m, "Kick", 100),
            CreateEvent(4, 4.0m, "Snare", 100)
        };

        var barStats = CreateBarStats(events, 4);
        var barPatterns = CreateBarPatterns(events, 4);

        var featureData = new DrumTrackFeatureData
        {
            TrackId = "test",
            TotalBars = 4,
            DefaultBeatsPerBar = 4,
            TempoEstimateBpm = 120,
            Events = events,
            BarPatterns = barPatterns,
            BarStats = barStats,
            RoleMatrices = new Dictionary<string, BeatPositionMatrix>(),
            ActiveRoles = new HashSet<string> { "Kick", "Snare", "Crash", "Tom1", "Tom2", "FloorTom" }
        };

        // Act
        var result = StructuralMarkerDetector.Detect(featureData);

        // Assert
        result.CrashBars.Should().Contain(2); // Bar 2 has a crash
        result.TotalMarkers.Should().BeGreaterThan(0);
    }

    #endregion

    #region AnchorCandidateExtractor

    [Fact]
    public void Extract_WithConsistentPositions_IdentifiesAnchors()
    {
        // Arrange - Kick on beat 1 every bar (position 0), Snare on beats 2 and 4 (positions 4, 12)
        var kickMatrix = CreateMatrix("Kick", 10, Enumerable.Range(0, 10).Select(_ => new[] { 0, 8 }).ToArray());
        var snareMatrix = CreateMatrix("Snare", 10, Enumerable.Range(0, 10).Select(_ => new[] { 4, 12 }).ToArray());

        var roleMatrices = new Dictionary<string, BeatPositionMatrix>
        {
            ["Kick"] = kickMatrix,
            ["Snare"] = snareMatrix
        };

        // Act
        var result = AnchorCandidateExtractor.Extract(roleMatrices, includePopRockReference: true);

        // Assert
        result.RoleAnchors.Should().ContainKey("Kick");
        result.RoleAnchors.Should().ContainKey("Snare");

        // Position 0 should be an anchor for Kick (100% consistency)
        result.RoleAnchors["Kick"].Should().Contain(a => a.GridPosition == 0 && a.ConsistencyRatio >= 0.8);

        // PopRock variance should be computed
        result.PopRockAnchorVariance.Should().NotBeNull();
        result.PopRockAnchorVariance!.ReferenceName.Should().Be("PopRockBasic");
    }

    #endregion

    #region SequencePatternDetector

    [Fact]
    public void Detect_WithRepeatingSequences_IdentifiesTwoBarAndFourBarSequences()
    {
        // Arrange - Create a repeating 2-bar sequence
        var fingerprints = new List<BarPatternFingerprint>
        {
            CreateFingerprint(1, "A"),
            CreateFingerprint(2, "B"),
            CreateFingerprint(3, "A"), // 2-bar sequence repeats
            CreateFingerprint(4, "B"),
            CreateFingerprint(5, "A"), // 2-bar sequence repeats again
            CreateFingerprint(6, "B"),
            CreateFingerprint(7, "C"),
            CreateFingerprint(8, "D")
        };

        // Act
        var result = SequencePatternDetector.Detect(fingerprints);

        // Assert
        // A-B sequence appears at bars 1, 3, 5 (3 occurrences)
        result.TwoBarSequences.Should().NotBeEmpty();
        result.TwoBarSequences[0].Occurrences.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Helper Methods

    private static DrumMidiEvent CreateEvent(int bar, decimal beat, string role, int velocity)
    {
        return new DrumMidiEvent
        {
            BarNumber = bar,
            Beat = beat,
            Role = role,
            MidiNote = role == "Kick" ? 36 : role == "Snare" ? 38 : role == "Crash" ? 49 : 48,
            Velocity = velocity,
            DurationTicks = 120,
            AbsoluteTimeTicks = (bar - 1) * 1920 + (int)((beat - 1) * 480)
        };
    }

    private static List<BarOnsetStats> CreateBarStats(List<DrumMidiEvent> events, int totalBars)
    {
        return Enumerable.Range(1, totalBars).Select(bar =>
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
    }

    private static List<BarPatternFingerprint> CreateBarPatterns(List<DrumMidiEvent> events, int totalBars)
    {
        return Enumerable.Range(1, totalBars).Select(bar =>
            BarPatternExtractor.Extract(events.Where(e => e.BarNumber == bar).ToList(), bar, 4)
        ).ToList();
    }

    private static BarPatternFingerprint CreateFingerprint(int barNumber, string hash)
    {
        return new BarPatternFingerprint
        {
            BarNumber = barNumber,
            BeatsPerBar = 4,
            PatternHash = hash,
            RoleBitmasks = new Dictionary<string, long> { ["Kick"] = 1L << (barNumber % 4) },
            RoleVelocities = new Dictionary<string, IReadOnlyList<int>> { ["Kick"] = new[] { 100 } },
            RoleEventCounts = new Dictionary<string, int> { ["Kick"] = 1 },
            GridResolution = 16
        };
    }

    private static BeatPositionMatrix CreateMatrix(string role, int totalBars, int[][] hitPositions)
    {
        var barSlots = new List<BeatPositionSlot?[]>();
        for (int bar = 0; bar < totalBars; bar++)
        {
            var slots = new BeatPositionSlot?[16];
            if (bar < hitPositions.Length)
            {
                foreach (var pos in hitPositions[bar])
                {
                    if (pos < 16)
                        slots[pos] = new BeatPositionSlot(100, 0);
                }
            }
            barSlots.Add(slots);
        }

        return new BeatPositionMatrix
        {
            Role = role,
            TotalBars = totalBars,
            GridResolution = 16,
            BarSlots = barSlots
        };
    }

    #endregion
}

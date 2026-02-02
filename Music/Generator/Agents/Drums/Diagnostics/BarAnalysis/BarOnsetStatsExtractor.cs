// AI: purpose=Extracts BarOnsetStats from drum events for a single bar (Story 7.2a).
// AI: invariants=Handles empty bars gracefully; all calculations deterministic; offbeat detection uses fractional beat.
// AI: deps=Consumes DrumMidiEvent list; outputs BarOnsetStats; used by DrumTrackFeatureDataBuilder.
// AI: change=Story 7.2a; extend for additional statistics as needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Extracts statistical summary from drum events for a single bar.
/// Calculates velocity, timing, and beat distribution metrics.
/// Story 7.2a: Per-Bar Statistics.
/// </summary>
public static class BarOnsetStatsExtractor
{
    /// <summary>
    /// Extracts statistics from events for a single bar.
    /// </summary>
    /// <param name="events">Drum events for this bar (should all have same BarNumber).</param>
    /// <param name="barNumber">Bar number (1-based).</param>
    /// <param name="beatsPerBar">Number of beats in this bar (from time signature).</param>
    /// <returns>Statistical summary for the bar.</returns>
    public static BarOnsetStats Extract(
        IReadOnlyList<DrumMidiEvent> events,
        int barNumber,
        int beatsPerBar)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Filter to events for this bar
        var barEvents = events.Where(e => e.BarNumber == barNumber).ToList();

        if (barEvents.Count == 0)
        {
            return CreateEmptyStats(barNumber, beatsPerBar);
        }

        // Calculate hits per role
        var hitsPerRole = barEvents
            .GroupBy(e => e.Role)
            .ToDictionary(g => g.Key, g => g.Count());

        // Velocity statistics
        var velocities = barEvents.Select(e => e.Velocity).ToList();
        var avgVelocity = velocities.Average();
        var minVelocity = velocities.Min();
        var maxVelocity = velocities.Max();

        var avgVelocityPerRole = barEvents
            .GroupBy(e => e.Role)
            .ToDictionary(g => g.Key, g => g.Average(e => e.Velocity));

        // Timing statistics
        var timingOffsets = barEvents
            .Select(e => e.TimingOffsetTicks ?? 0)
            .ToList();

        var avgTimingOffset = timingOffsets.Average();
        var minTimingOffset = timingOffsets.Min();
        var maxTimingOffset = timingOffsets.Max();

        // Beat distribution
        var hitsPerBeat = CalculateHitsPerBeat(barEvents, beatsPerBar);

        // Offbeat ratio
        var offbeatRatio = CalculateOffbeatRatio(barEvents);

        return new BarOnsetStats
        {
            BarNumber = barNumber,
            TotalHits = barEvents.Count,
            HitsPerRole = hitsPerRole,
            AverageVelocity = avgVelocity,
            MinVelocity = minVelocity,
            MaxVelocity = maxVelocity,
            AverageVelocityPerRole = avgVelocityPerRole,
            AverageTimingOffset = avgTimingOffset,
            MinTimingOffset = minTimingOffset,
            MaxTimingOffset = maxTimingOffset,
            HitsPerBeat = hitsPerBeat,
            OffbeatRatio = offbeatRatio
        };
    }

    /// <summary>
    /// Creates empty statistics for a bar with no events.
    /// </summary>
    private static BarOnsetStats CreateEmptyStats(int barNumber, int beatsPerBar)
    {
        var emptyHitsPerBeat = Enumerable.Repeat(0, beatsPerBar).ToList();

        return new BarOnsetStats
        {
            BarNumber = barNumber,
            TotalHits = 0,
            HitsPerRole = new Dictionary<string, int>(),
            AverageVelocity = 0,
            MinVelocity = 0,
            MaxVelocity = 0,
            AverageVelocityPerRole = new Dictionary<string, double>(),
            AverageTimingOffset = 0,
            MinTimingOffset = 0,
            MaxTimingOffset = 0,
            HitsPerBeat = emptyHitsPerBeat,
            OffbeatRatio = 0
        };
    }

    /// <summary>
    /// Calculates hit count per beat position.
    /// </summary>
    private static IReadOnlyList<int> CalculateHitsPerBeat(
        IReadOnlyList<DrumMidiEvent> events,
        int beatsPerBar)
    {
        var hitsPerBeat = new int[beatsPerBar];

        foreach (var evt in events)
        {
            // Convert 1-based beat to 0-based beat index
            // Truncate fractional part to get which beat this falls on
            var beatIndex = (int)Math.Floor(evt.Beat - 1m);

            // Clamp to valid range
            beatIndex = Math.Clamp(beatIndex, 0, beatsPerBar - 1);

            hitsPerBeat[beatIndex]++;
        }

        return hitsPerBeat;
    }

    /// <summary>
    /// Calculates the ratio of hits that are not on downbeats.
    /// Downbeat = integer beat position (1.0, 2.0, 3.0, 4.0 in 4/4).
    /// </summary>
    private static double CalculateOffbeatRatio(IReadOnlyList<DrumMidiEvent> events)
    {
        if (events.Count == 0)
            return 0;

        // Tolerance for "on beat" detection (within 1/16th of a beat)
        const decimal onBeatTolerance = 0.0625m;

        var offbeatCount = events.Count(e =>
        {
            // Check if beat is close to an integer value
            var fractionalPart = e.Beat - Math.Floor(e.Beat);
            return fractionalPart > onBeatTolerance && fractionalPart < (1m - onBeatTolerance);
        });

        return (double)offbeatCount / events.Count;
    }

    /// <summary>
    /// Extracts statistics for all bars in the event list.
    /// </summary>
    /// <param name="events">All drum events.</param>
    /// <param name="barTrack">BarTrack for timing info.</param>
    /// <returns>List of statistics, one per bar.</returns>
    public static IReadOnlyList<BarOnsetStats> ExtractAllBars(
        IReadOnlyList<DrumMidiEvent> events,
        BarTrack barTrack)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(barTrack);

        var statsList = new List<BarOnsetStats>();

        foreach (var bar in barTrack.Bars)
        {
            var stats = Extract(events, bar.BarNumber, bar.BeatsPerBar);
            statsList.Add(stats);
        }

        return statsList;
    }
}

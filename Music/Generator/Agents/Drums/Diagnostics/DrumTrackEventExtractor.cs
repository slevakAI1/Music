// AI: purpose=Extracts raw drum events from PartTrack+BarTrack into DrumMidiEvent list (Story 7.2a).
// AI: invariants=Output sorted by AbsoluteTimeTicks; computes TimingOffsetTicks from nearest grid; handles variable time signatures.
// AI: deps=Consumes PartTrack, BarTrack, DrumRoleMapper; outputs IReadOnlyList<DrumMidiEvent>.
// AI: change=Story 7.2a; extend grid resolution or timing logic as needed for future analysis.

using Music.Generator;
using Music.MyMidi;

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Extracts raw drum events from a PartTrack into normalized DrumMidiEvent format.
/// Handles variable time signatures and computes timing offset from quantized grid.
/// Story 7.2a: Raw Event Extraction.
/// </summary>
public static class DrumTrackEventExtractor
{
    /// <summary>
    /// Default grid resolution for timing offset calculation (16th notes).
    /// Value represents the denominator: 16 = 16th note grid.
    /// </summary>
    public const int DefaultGridResolution = 16;

    /// <summary>
    /// Extracts all drum events from a PartTrack, mapping to normalized format.
    /// </summary>
    /// <param name="partTrack">Source drum PartTrack with note events.</param>
    /// <param name="barTrack">BarTrack providing timing context (must be populated).</param>
    /// <param name="gridResolution">Grid resolution for timing offset calculation (default: 16 for 16th notes).</param>
    /// <returns>Sorted list of DrumMidiEvent, ordered by AbsoluteTimeTicks.</returns>
    /// <exception cref="ArgumentNullException">If partTrack or barTrack is null.</exception>
    public static IReadOnlyList<DrumMidiEvent> Extract(
        PartTrack partTrack,
        BarTrack barTrack,
        int gridResolution = DefaultGridResolution)
    {
        ArgumentNullException.ThrowIfNull(partTrack);
        ArgumentNullException.ThrowIfNull(barTrack);

        if (barTrack.Bars.Count == 0)
            return Array.Empty<DrumMidiEvent>();

        var events = new List<DrumMidiEvent>();

        foreach (var noteEvent in partTrack.PartTrackNoteEvents)
        {
            // Only process NoteOn events (skip meta events, control changes, etc.)
            if (noteEvent.Type != PartTrackEventType.NoteOn)
                continue;

            var midiNote = noteEvent.NoteNumber;
            var absoluteTicks = noteEvent.AbsoluteTimeTicks;
            var velocity = noteEvent.NoteOnVelocity;
            var durationTicks = noteEvent.NoteDurationTicks;

            // Find which bar contains this tick
            var bar = FindBarForTick(barTrack, absoluteTicks);
            if (bar == null)
                continue; // Event outside of known bar range

            // Calculate beat position within bar
            var beat = CalculateBeatInBar(bar, absoluteTicks);

            // Map MIDI note to role
            var role = DrumRoleMapper.MapNoteToRole(midiNote);

            // Calculate timing offset from nearest grid position
            var timingOffset = CalculateTimingOffset(bar, absoluteTicks, gridResolution);

            var drumEvent = new DrumMidiEvent
            {
                BarNumber = bar.BarNumber,
                Beat = beat,
                Role = role,
                MidiNote = midiNote,
                Velocity = velocity,
                DurationTicks = durationTicks,
                AbsoluteTimeTicks = absoluteTicks,
                TimingOffsetTicks = timingOffset
            };

            events.Add(drumEvent);
        }

        // Sort by absolute time for deterministic ordering
        events.Sort((a, b) => a.AbsoluteTimeTicks.CompareTo(b.AbsoluteTimeTicks));

        return events;
    }

    /// <summary>
    /// Finds the bar containing the given absolute tick position.
    /// Returns null if tick is outside all bars.
    /// </summary>
    private static Bar? FindBarForTick(BarTrack barTrack, long absoluteTick)
    {
        // Binary search would be more efficient for large bar counts,
        // but linear scan is sufficient for typical song lengths (< 500 bars)
        foreach (var bar in barTrack.Bars)
        {
            if (absoluteTick >= bar.StartTick && absoluteTick < bar.EndTick)
                return bar;
        }

        return null;
    }

    /// <summary>
    /// Calculates the beat position (1-based, fractional) within a bar.
    /// </summary>
    private static decimal CalculateBeatInBar(Bar bar, long absoluteTick)
    {
        // Offset from bar start in ticks
        var ticksFromBarStart = absoluteTick - bar.StartTick;

        // Convert to beat units (1-based)
        // Each beat is TicksPerBeat ticks
        var beatOffset = (decimal)ticksFromBarStart / bar.TicksPerBeat;

        // Add 1 for 1-based beat numbering
        return beatOffset + 1m;
    }

    /// <summary>
    /// Calculates timing offset from nearest quantized grid position.
    /// Positive = behind grid (late), Negative = ahead of grid (early).
    /// </summary>
    private static int CalculateTimingOffset(Bar bar, long absoluteTick, int gridResolution)
    {
        // Calculate ticks per grid position
        // For 16th notes in 4/4: TicksPerQuarterNote / 4 = 120 ticks (with 480 TPQ)
        var ticksPerGridPosition = MusicConstants.TicksPerQuarterNote * 4 / gridResolution;

        // Find tick offset within bar
        var ticksFromBarStart = absoluteTick - bar.StartTick;

        // Find nearest grid position
        var gridPositionIndex = (int)Math.Round((double)ticksFromBarStart / ticksPerGridPosition);
        var nearestGridTick = gridPositionIndex * ticksPerGridPosition;

        // Calculate offset (positive = behind grid, negative = ahead)
        var offset = ticksFromBarStart - nearestGridTick;

        return (int)offset;
    }

    /// <summary>
    /// Extracts events for a single bar.
    /// Useful for per-bar pattern analysis.
    /// </summary>
    /// <param name="allEvents">Pre-extracted list of all events.</param>
    /// <param name="barNumber">Bar number (1-based) to filter by.</param>
    /// <returns>Events occurring in the specified bar.</returns>
    public static IReadOnlyList<DrumMidiEvent> GetEventsForBar(
        IReadOnlyList<DrumMidiEvent> allEvents,
        int barNumber)
    {
        ArgumentNullException.ThrowIfNull(allEvents);

        return allEvents.Where(e => e.BarNumber == barNumber).ToList();
    }

    /// <summary>
    /// Groups events by bar number for batch processing.
    /// </summary>
    /// <param name="events">All extracted events.</param>
    /// <returns>Dictionary of bar number to events in that bar.</returns>
    public static IReadOnlyDictionary<int, IReadOnlyList<DrumMidiEvent>> GroupByBar(
        IReadOnlyList<DrumMidiEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events
            .GroupBy(e => e.BarNumber)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<DrumMidiEvent>)g.ToList());
    }
}

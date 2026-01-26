// AI: purpose=Raw MIDI drum event extracted from PartTrack for feature analysis (Story 7.2a).
// AI: invariants=BarNumber is 1-based; Beat is 1-based fractional; Velocity is 1-127; AbsoluteTimeTicks is authoritative.
// AI: deps=Used by DrumTrackEventExtractor, BarPatternExtractor, BarOnsetStatsExtractor; serializable to JSON.
// AI: change=Story 7.2a; add fields only if needed for downstream analysis; keep immutable for thread safety.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Represents a single raw MIDI drum event extracted from a PartTrack.
/// Used as input for pattern fingerprinting, statistics, and feature extraction.
/// Story 7.2a: Raw Event Extraction.
/// </summary>
public sealed record DrumMidiEvent
{
    /// <summary>
    /// Bar number (1-based) where this event occurs.
    /// </summary>
    public required int BarNumber { get; init; }

    /// <summary>
    /// Beat position within the bar (1-based, fractional).
    /// Example: 1.0 = beat 1, 2.5 = beat 2 and a half.
    /// </summary>
    public required decimal Beat { get; init; }

    /// <summary>
    /// Normalized role name (e.g., "Kick", "Snare", "ClosedHat").
    /// Mapped from MIDI note using DrumRoleMapper.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Original GM2 MIDI note number (typically 36-81 for drums).
    /// </summary>
    public required int MidiNote { get; init; }

    /// <summary>
    /// MIDI velocity (1-127).
    /// </summary>
    public required int Velocity { get; init; }

    /// <summary>
    /// Duration of the note in ticks.
    /// </summary>
    public required int DurationTicks { get; init; }

    /// <summary>
    /// Absolute tick position from track start.
    /// Authoritative timing reference.
    /// </summary>
    public required long AbsoluteTimeTicks { get; init; }

    /// <summary>
    /// Deviation from nearest quantized grid position in ticks.
    /// Positive = behind grid, Negative = ahead of grid.
    /// Null if not computed.
    /// </summary>
    public int? TimingOffsetTicks { get; init; }
}

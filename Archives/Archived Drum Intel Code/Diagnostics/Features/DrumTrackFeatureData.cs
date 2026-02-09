// AI: purpose=Top-level container for all extracted drum track features (Story 7.2a).
// AI: invariants=Events sorted by AbsoluteTimeTicks; all collections non-null; TotalBars matches BarPatterns/BarStats count.
// AI: deps=Populated by DrumTrackFeatureDataBuilder; serializable via DrumFeatureDataSerializer.
// AI: change=Story 7.2a; extend with Story 7.2b pattern analysis data in future.

using Music.Generator.Drums.Diagnostics;
using Music.Generator.Drums.Diagnostics.BarAnalysis;

namespace Music.Generator.Drums.Diagnostics.Features;

/// <summary>
/// Top-level container for all extracted drum track feature data.
/// Contains raw events, per-bar patterns, statistics, and role matrices.
/// Designed for serialization and downstream analysis.
/// Story 7.2a: Track-Level Container.
/// </summary>
public sealed record DrumTrackFeatureData
{
    // --- Metadata ---

    /// <summary>
    /// Unique identifier for this track extraction.
    /// Used to correlate with source files and analysis results.
    /// </summary>
    public required string TrackId { get; init; }

    /// <summary>
    /// User-provided genre hint (e.g., "PopRock", "Jazz").
    /// Null if not specified.
    /// </summary>
    public string? GenreHint { get; init; }

    /// <summary>
    /// User-provided artist hint (e.g., "Led Zeppelin").
    /// Null if not specified.
    /// </summary>
    public string? ArtistHint { get; init; }

    /// <summary>
    /// Total number of bars in the analyzed track.
    /// </summary>
    public required int TotalBars { get; init; }

    /// <summary>
    /// Default beats per bar (most common time signature).
    /// </summary>
    public required int DefaultBeatsPerBar { get; init; }

    /// <summary>
    /// Estimated tempo in BPM (beats per minute).
    /// May be 0 if not available from source.
    /// </summary>
    public required int TempoEstimateBpm { get; init; }

    /// <summary>
    /// Schema version for serialization compatibility.
    /// Increment when breaking changes are made to the format.
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>
    /// Timestamp when this extraction was performed.
    /// </summary>
    public DateTimeOffset ExtractionTimestamp { get; init; } = DateTimeOffset.UtcNow;

    // --- Raw Events ---

    /// <summary>
    /// All drum hits in the track, sorted by AbsoluteTimeTicks.
    /// </summary>
    public required IReadOnlyList<DrumMidiEvent> Events { get; init; }

    // --- Per-Bar Data ---

    /// <summary>
    /// Pattern fingerprint for each bar.
    /// One entry per bar in the track.
    /// </summary>
    public required IReadOnlyList<BarPatternFingerprint> BarPatterns { get; init; }

    /// <summary>
    /// Statistical summary for each bar.
    /// One entry per bar in the track.
    /// </summary>
    public required IReadOnlyList<BarOnsetStats> BarStats { get; init; }

    // --- Per-Role Matrices ---

    /// <summary>
    /// Beat position matrices per role.
    /// Key: role name (e.g., "Kick", "Snare").
    /// </summary>
    public required IReadOnlyDictionary<string, BeatPositionMatrix> RoleMatrices { get; init; }

    // --- Role Summary ---

    /// <summary>
    /// Set of all roles present in this track.
    /// </summary>
    public required IReadOnlySet<string> ActiveRoles { get; init; }

    // --- Derived Properties ---

    /// <summary>
    /// Total hit count across all bars.
    /// </summary>
    public int TotalHits => Events.Count;

    /// <summary>
    /// Average hits per bar.
    /// </summary>
    public double AverageHitsPerBar => TotalBars > 0 ? (double)TotalHits / TotalBars : 0;

    /// <summary>
    /// Number of unique patterns in the track (based on PatternHash).
    /// </summary>
    public int UniquePatternCount => BarPatterns.Select(p => p.PatternHash).Distinct().Count();

    /// <summary>
    /// Average velocity across all events.
    /// </summary>
    public double AverageVelocity => Events.Count > 0 ? Events.Average(e => e.Velocity) : 0;

    /// <summary>
    /// Velocity standard deviation across all events.
    /// </summary>
    public double VelocityStandardDeviation
    {
        get
        {
            if (Events.Count == 0) return 0;
            var avg = AverageVelocity;
            var sumOfSquares = Events.Sum(e => (e.Velocity - avg) * (e.Velocity - avg));
            return Math.Sqrt(sumOfSquares / Events.Count);
        }
    }

    /// <summary>
    /// Average timing offset across all events.
    /// </summary>
    public double AverageTimingOffset => Events.Count > 0
        ? Events.Average(e => e.TimingOffsetTicks ?? 0)
        : 0;
}

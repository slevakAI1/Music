// AI: purpose=Per-bar onset statistics for density/velocity/timing analysis (Story 7.2a).
// AI: invariants=All statistics computed from events in a single bar; OffbeatRatio in [0,1].
// AI: deps=Populated by BarOnsetStatsExtractor; used for aggregate analysis in DrumTrackFeatureData.
// AI: change=Story 7.2a; add additional stats as needed for downstream analysis.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Statistical summary of drum onsets within a single bar.
/// Captures density, velocity distribution, timing characteristics, and beat distribution.
/// Story 7.2a: Per-Bar Statistics.
/// </summary>
public sealed record BarOnsetStats
{
    /// <summary>
    /// Bar number (1-based) this statistics applies to.
    /// </summary>
    public required int BarNumber { get; init; }

    /// <summary>
    /// Total number of hits in this bar across all roles.
    /// </summary>
    public required int TotalHits { get; init; }

    /// <summary>
    /// Hit count per role in this bar.
    /// Key: role name, Value: hit count.
    /// </summary>
    public required IReadOnlyDictionary<string, int> HitsPerRole { get; init; }

    // --- Velocity Statistics ---

    /// <summary>
    /// Average velocity across all hits in this bar.
    /// </summary>
    public required double AverageVelocity { get; init; }

    /// <summary>
    /// Minimum velocity in this bar (0 if no hits).
    /// </summary>
    public required int MinVelocity { get; init; }

    /// <summary>
    /// Maximum velocity in this bar (0 if no hits).
    /// </summary>
    public required int MaxVelocity { get; init; }

    /// <summary>
    /// Average velocity per role in this bar.
    /// Key: role name, Value: average velocity.
    /// </summary>
    public required IReadOnlyDictionary<string, double> AverageVelocityPerRole { get; init; }

    // --- Timing Statistics ---

    /// <summary>
    /// Average timing offset from grid across all hits.
    /// Positive = behind grid, Negative = ahead of grid.
    /// </summary>
    public required double AverageTimingOffset { get; init; }

    /// <summary>
    /// Minimum timing offset in this bar.
    /// </summary>
    public required int MinTimingOffset { get; init; }

    /// <summary>
    /// Maximum timing offset in this bar.
    /// </summary>
    public required int MaxTimingOffset { get; init; }

    // --- Beat Distribution ---

    /// <summary>
    /// Hit count per beat position (0-based index = beat number - 1).
    /// For 4/4: index 0 = beat 1, index 1 = beat 2, etc.
    /// </summary>
    public required IReadOnlyList<int> HitsPerBeat { get; init; }

    /// <summary>
    /// Ratio of hits not on downbeats (offbeat ratio).
    /// 0.0 = all hits on downbeats, 1.0 = no hits on downbeats.
    /// Downbeats defined as integer beat positions (1, 2, 3, 4 in 4/4).
    /// </summary>
    public required double OffbeatRatio { get; init; }

    // --- Derived Properties ---

    /// <summary>
    /// Velocity range (max - min) in this bar.
    /// Indicates dynamic variation.
    /// </summary>
    public int VelocityRange => MaxVelocity - MinVelocity;

    /// <summary>
    /// Timing offset range (max - min) in ticks.
    /// Indicates timing variation/looseness.
    /// </summary>
    public int TimingOffsetRange => MaxTimingOffset - MinTimingOffset;

    /// <summary>
    /// Whether this bar has any hits.
    /// </summary>
    public bool HasHits => TotalHits > 0;
}

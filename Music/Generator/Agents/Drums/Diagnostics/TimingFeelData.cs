// AI: purpose=Timing feel data for analyzing swing, pocket, and timing consistency (Story 7.2b).
// AI: invariants=SwingRatio 1.0=straight, 2.0=triplet swing; TimingConsistency in [0,1].
// AI: deps=Populated by TimingFeelExtractor; uses DrumMidiEvent timing offset data.
// AI: change=Story 7.2b; add additional timing metrics as needed.

namespace Music.Generator.Agents.Drums.Diagnostics;

/// <summary>
/// Distribution of timing offsets for statistical analysis.
/// Story 7.2b: Timing Feel Data.
/// </summary>
/// <param name="Mean">Mean timing offset (positive = behind, negative = ahead).</param>
/// <param name="StdDev">Standard deviation of offsets.</param>
/// <param name="MinOffset">Minimum offset in ticks.</param>
/// <param name="MaxOffset">Maximum offset in ticks.</param>
/// <param name="Histogram">Buckets from -20 to +20 ticks (5 tick width each).</param>
public sealed record TimingDistribution(
    double Mean,
    double StdDev,
    int MinOffset,
    int MaxOffset,
    IReadOnlyList<int> Histogram);

/// <summary>
/// Data about timing feel in a drum track.
/// Captures swing ratio, pocket feel, and per-role timing characteristics.
/// Story 7.2b: Timing Feel Data.
/// </summary>
public sealed record TimingFeelData
{
    /// <summary>
    /// Average timing offset per role.
    /// Key: role name, Value: average offset in ticks.
    /// Positive = behind the beat, Negative = ahead.
    /// </summary>
    public required IReadOnlyDictionary<string, double> RoleAverageOffset { get; init; }

    /// <summary>
    /// Timing offset distribution per role.
    /// Key: role name, Value: distribution statistics.
    /// </summary>
    public required IReadOnlyDictionary<string, TimingDistribution> RoleTimingDistributions { get; init; }

    /// <summary>
    /// Swing ratio detected in the track.
    /// 1.0 = straight 8ths, ~1.5 = light swing, ~2.0 = triplet swing.
    /// </summary>
    public required double SwingRatio { get; init; }

    /// <summary>
    /// Overall ahead/behind score.
    /// Negative = pushing ahead, Positive = laying back.
    /// </summary>
    public required double AheadBehindScore { get; init; }

    /// <summary>
    /// Timing consistency (0.0-1.0).
    /// Higher = more consistent timing, Lower = more loose.
    /// </summary>
    public required double TimingConsistency { get; init; }

    /// <summary>
    /// Whether the track has swing feel (ratio >= 1.2).
    /// </summary>
    public bool HasSwing => SwingRatio >= 1.2;

    /// <summary>
    /// Whether the track feels laid back (average offset > 5 ticks behind).
    /// </summary>
    public bool IsLaidBack => AheadBehindScore > 5.0;

    /// <summary>
    /// Whether the track feels pushed/rushed (average offset < -5 ticks).
    /// </summary>
    public bool IsPushed => AheadBehindScore < -5.0;
}
